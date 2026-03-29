using Contracts.IntegrationEvents.Identity;
using Integration.Tests.Factories;
using Integration.Tests.Fixtures;
using Integration.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductCatalog.Infrastructure.Persistence;
using Shouldly;
using TestCommon;
using Wolverine;
using Wolverine.Tracking;
using Xunit;

namespace Integration.Tests.Events;

[Trait("Category", TestConstants.CategoryName)]
[Collection(TestConstants.CollectionName)]
public sealed class TenantDeactivatedEventFlowTests : IAsyncLifetime
{
    private readonly SharedContainers _containers;
    private IdentityServiceFactory _identityFactory = null!;
    private ProductCatalogServiceFactory _productCatalogFactory = null!;

    public TenantDeactivatedEventFlowTests(SharedContainers containers)
    {
        _containers = containers;
    }

    public async ValueTask InitializeAsync()
    {
        _identityFactory = new IdentityServiceFactory(_containers);
        _productCatalogFactory = new ProductCatalogServiceFactory(_containers);

        await Task.WhenAll(
            _identityFactory.InitializeAsync().AsTask(),
            _productCatalogFactory.InitializeAsync().AsTask()
        );
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _productCatalogFactory.DisposeAsync().AsTask(),
            _identityFactory.DisposeAsync().AsTask()
        );
    }

    [Fact]
    public async Task TenantDeactivatedEvent_CascadeDeletesProductsAndCategories()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();

        await using (AsyncServiceScope scope = _productCatalogFactory.Services.CreateAsyncScope())
        {
            ProductCatalogDbContext db =
                scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();
            db.Categories.Add(
                new ProductCatalog.Domain.Entities.Category
                {
                    Id = Guid.NewGuid(),
                    Name = "Deactivation Test Category",
                    TenantId = tenantId,
                    Audit = TestDataHelper.CreateAudit(actorId),
                }
            );
            db.Products.Add(
                new ProductCatalog.Domain.Entities.Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Deactivation Test Product",
                    Price = 50.00m,
                    TenantId = tenantId,
                    Audit = TestDataHelper.CreateAudit(actorId),
                }
            );
            await db.SaveChangesAsync();
        }

        TenantDeactivatedIntegrationEvent integrationEvent = new(
            CorrelationId: Guid.NewGuid(),
            TenantId: tenantId,
            ActorId: actorId,
            OccurredAtUtc: DateTime.UtcNow
        );

        IHost identityHost = _identityFactory.Services.GetRequiredService<IHost>();
        IHost productCatalogHost = _productCatalogFactory.Services.GetRequiredService<IHost>();

        CancellationToken ct = TestContext.Current.CancellationToken;

        // Act
        ITrackedSession session = await identityHost
            .TrackActivity()
            .Timeout(TestConstants.TrackedSessionTimeout)
            .IncludeExternalTransports()
            .AlsoTrack(productCatalogHost)
            .WaitForMessageToBeReceivedAt<TenantDeactivatedIntegrationEvent>(productCatalogHost)
            .PublishMessageAndWaitAsync(integrationEvent);

        // Assert
        session.Received.MessagesOf<TenantDeactivatedIntegrationEvent>().ShouldNotBeEmpty();

        await AsyncPoll.UntilTrueAsync(
            async () =>
            {
                await using AsyncServiceScope scope2 =
                    _productCatalogFactory.Services.CreateAsyncScope();
                ProductCatalogDbContext db2 =
                    scope2.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();

                int deletedProductCount = await db2
                    .Products.IgnoreQueryFilters()
                    .CountAsync(p => p.TenantId == tenantId && p.IsDeleted, ct);
                int deletedCategoryCount = await db2
                    .Categories.IgnoreQueryFilters()
                    .CountAsync(c => c.TenantId == tenantId && c.IsDeleted, ct);
                return deletedProductCount > 0 && deletedCategoryCount > 0;
            },
            TestConstants.TrackedSessionTimeout,
            cancellationToken: ct
        );

        await using AsyncServiceScope scope3 = _productCatalogFactory.Services.CreateAsyncScope();
        ProductCatalogDbContext db3 =
            scope3.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();

        int finalProductCount = await db3
            .Products.IgnoreQueryFilters()
            .CountAsync(p => p.TenantId == tenantId && p.IsDeleted, ct);
        finalProductCount.ShouldBeGreaterThan(0);

        int finalCategoryCount = await db3
            .Categories.IgnoreQueryFilters()
            .CountAsync(c => c.TenantId == tenantId && c.IsDeleted, ct);
        finalCategoryCount.ShouldBeGreaterThan(0);
    }
}
