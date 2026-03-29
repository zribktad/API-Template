using Contracts.IntegrationEvents.ProductCatalog;
using Integration.Tests.Factories;
using Integration.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;
using Shouldly;
using TestCommon;
using Wolverine;
using Wolverine.Tracking;
using Xunit;

namespace Integration.Tests.Events;

[Trait("Category", TestConstants.CategoryName)]
[Collection(TestConstants.CollectionName)]
public sealed class ProductCreatedEventFlowTests : IAsyncLifetime
{
    private readonly SharedContainers _containers;
    private ProductCatalogServiceFactory _productCatalogFactory = null!;
    private ReviewsServiceFactory _reviewsFactory = null!;

    public ProductCreatedEventFlowTests(SharedContainers containers)
    {
        _containers = containers;
    }

    public async ValueTask InitializeAsync()
    {
        _productCatalogFactory = new ProductCatalogServiceFactory(_containers);
        _reviewsFactory = new ReviewsServiceFactory(_containers);

        await Task.WhenAll(
            _productCatalogFactory.InitializeAsync().AsTask(),
            _reviewsFactory.InitializeAsync().AsTask()
        );
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _reviewsFactory.DisposeAsync().AsTask(),
            _productCatalogFactory.DisposeAsync().AsTask()
        );
    }

    [Fact]
    public async Task ProductCreatedEvent_CreatesProjectionInReviewsWithCorrectTenantId()
    {
        // Arrange
        Guid productId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        string productName = "Integration Test Product";

        ProductCreatedIntegrationEvent integrationEvent = new(
            ProductId: productId,
            TenantId: tenantId,
            Name: productName,
            OccurredAtUtc: DateTime.UtcNow
        );

        IHost productCatalogHost = _productCatalogFactory.Services.GetRequiredService<IHost>();
        IHost reviewsHost = _reviewsFactory.Services.GetRequiredService<IHost>();

        // Act
        CancellationToken ct = TestContext.Current.CancellationToken;

        ITrackedSession session = await productCatalogHost
            .TrackActivity()
            .Timeout(TestConstants.TrackedSessionTimeout)
            .IncludeExternalTransports()
            .AlsoTrack(reviewsHost)
            .WaitForMessageToBeReceivedAt<ProductCreatedIntegrationEvent>(reviewsHost)
            .PublishMessageAndWaitAsync(integrationEvent);

        // Assert
        session.Received.MessagesOf<ProductCreatedIntegrationEvent>().ShouldNotBeEmpty();

        ProductProjection projection = await AsyncPoll.UntilNotNullAsync(
            async () =>
            {
                await using AsyncServiceScope scope = _reviewsFactory.Services.CreateAsyncScope();
                ReviewsDbContext db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
                return await db
                    .ProductProjections.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.ProductId == productId, ct);
            },
            TestConstants.TrackedSessionTimeout,
            cancellationToken: ct
        );
        projection.Name.ShouldBe(productName);
        // Verifies TenantAwareEnvelopeMapper propagated x-tenant-id through RabbitMQ
        projection.TenantId.ShouldBe(tenantId);
        projection.IsActive.ShouldBeTrue();
    }
}
