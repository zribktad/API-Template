using Contracts.IntegrationEvents.Identity;
using Contracts.IntegrationEvents.Sagas;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
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

namespace Integration.Tests.Sagas;

[Trait("Category", TestConstants.CategoryName)]
[Collection(TestConstants.CollectionName)]
public sealed class TenantDeactivationSagaIntegrationTests : IAsyncLifetime
{
    private readonly SharedContainers _containers;
    private IdentityServiceFactory _identityFactory = null!;
    private ProductCatalogServiceFactory _productCatalogFactory = null!;

    public TenantDeactivationSagaIntegrationTests(SharedContainers containers)
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
    public async Task TenantDeactivationSaga_CompletesWhenAllCascadesFinish()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid correlationId = Guid.NewGuid();

        await using (AsyncServiceScope scope = _identityFactory.Services.CreateAsyncScope())
        {
            IdentityDbContext db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.Tenants.Add(
                new Tenant
                {
                    Id = tenantId,
                    TenantId = tenantId,
                    Code = $"test-{tenantId:N}",
                    Name = "Test Tenant",
                    IsActive = true,
                    Audit = TestDataHelper.CreateAudit(actorId),
                }
            );
            db.Users.Add(
                new AppUser
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Username = "testuser",
                    Email = "testuser@example.com",
                    IsActive = true,
                    Role = UserRole.User,
                    Audit = TestDataHelper.CreateAudit(actorId),
                }
            );
            await db.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = _productCatalogFactory.Services.CreateAsyncScope())
        {
            ProductCatalogDbContext db =
                scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();
            db.Categories.Add(
                new ProductCatalog.Domain.Entities.Category
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Category",
                    TenantId = tenantId,
                    Audit = TestDataHelper.CreateAudit(actorId),
                }
            );
            db.Products.Add(
                new ProductCatalog.Domain.Entities.Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Product",
                    Price = 25.00m,
                    TenantId = tenantId,
                    Audit = TestDataHelper.CreateAudit(actorId),
                }
            );
            await db.SaveChangesAsync();
        }

        // Act
        IHost identityHost = _identityFactory.Services.GetRequiredService<IHost>();
        IHost productCatalogHost = _productCatalogFactory.Services.GetRequiredService<IHost>();

        CancellationToken ct = TestContext.Current.CancellationToken;

        ITrackedSession session = await identityHost
            .TrackActivity()
            .Timeout(TestConstants.TrackedSessionTimeout)
            .IncludeExternalTransports()
            .AlsoTrack(productCatalogHost)
            .WaitForMessageToBeReceivedAt<UsersCascadeCompleted>(identityHost)
            .WaitForMessageToBeReceivedAt<ProductsCascadeCompleted>(identityHost)
            .WaitForMessageToBeReceivedAt<CategoriesCascadeCompleted>(identityHost)
            .InvokeMessageAndWaitAsync(
                new StartTenantDeactivationSaga(correlationId, tenantId, actorId)
            );

        // Assert
        session.Sent.MessagesOf<TenantDeactivatedIntegrationEvent>().ShouldNotBeEmpty();
        session.Received.MessagesOf<UsersCascadeCompleted>().ShouldNotBeEmpty();
        session.Received.MessagesOf<ProductsCascadeCompleted>().ShouldNotBeEmpty();
        session.Received.MessagesOf<CategoriesCascadeCompleted>().ShouldNotBeEmpty();

        await AsyncPoll.UntilTrueAsync(
            async () =>
            {
                await using AsyncServiceScope scope = _identityFactory.Services.CreateAsyncScope();
                IdentityDbContext db =
                    scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                int deletedUserCount = await db
                    .Users.IgnoreQueryFilters()
                    .CountAsync(u => u.TenantId == tenantId && u.IsDeleted, ct);
                return deletedUserCount > 0;
            },
            TestConstants.TrackedSessionTimeout,
            cancellationToken: ct
        );

        await AsyncPoll.UntilTrueAsync(
            async () =>
            {
                await using AsyncServiceScope scope =
                    _productCatalogFactory.Services.CreateAsyncScope();
                ProductCatalogDbContext db =
                    scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();
                int deletedProductCount = await db
                    .Products.IgnoreQueryFilters()
                    .CountAsync(p => p.TenantId == tenantId && p.IsDeleted, ct);
                int deletedCategoryCount = await db
                    .Categories.IgnoreQueryFilters()
                    .CountAsync(c => c.TenantId == tenantId && c.IsDeleted, ct);
                return deletedProductCount > 0 && deletedCategoryCount > 0;
            },
            TestConstants.TrackedSessionTimeout,
            cancellationToken: ct
        );
    }
}
