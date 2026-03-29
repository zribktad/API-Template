using Contracts.IntegrationEvents.ProductCatalog;
using Contracts.IntegrationEvents.Sagas;
using Integration.Tests.Factories;
using Integration.Tests.Fixtures;
using Integration.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductCatalog.Infrastructure.Persistence;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;
using Shouldly;
using Wolverine;
using Wolverine.Tracking;
using Xunit;

namespace Integration.Tests.Sagas;

[Trait("Category", TestConstants.CategoryName)]
[Collection(TestConstants.CollectionName)]
public sealed class ProductDeletionSagaIntegrationTests : IAsyncLifetime
{
    private readonly SharedContainers _containers;
    private ProductCatalogServiceFactory _productCatalogFactory = null!;
    private ReviewsServiceFactory _reviewsFactory = null!;
    private FileStorageServiceFactory _fileStorageFactory = null!;

    public ProductDeletionSagaIntegrationTests(SharedContainers containers)
    {
        _containers = containers;
    }

    public async ValueTask InitializeAsync()
    {
        _productCatalogFactory = new ProductCatalogServiceFactory(_containers);
        _reviewsFactory = new ReviewsServiceFactory(_containers);
        _fileStorageFactory = new FileStorageServiceFactory(_containers);

        await Task.WhenAll(
            _productCatalogFactory.InitializeAsync().AsTask(),
            _reviewsFactory.InitializeAsync().AsTask(),
            _fileStorageFactory.InitializeAsync().AsTask()
        );
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _fileStorageFactory.DisposeAsync().AsTask(),
            _reviewsFactory.DisposeAsync().AsTask(),
            _productCatalogFactory.DisposeAsync().AsTask()
        );
    }

    [Fact]
    public async Task ProductDeletionSaga_CompletesWhenAllCascadesFinish()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Guid correlationId = Guid.NewGuid();

        await using (AsyncServiceScope scope = _productCatalogFactory.Services.CreateAsyncScope())
        {
            ProductCatalogDbContext db =
                scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();
            db.Products.Add(
                new ProductCatalog.Domain.Entities.Product
                {
                    Id = productId,
                    Name = "Test Product",
                    Price = 10.00m,
                    TenantId = tenantId,
                    Audit = TestDataHelper.CreateAudit(actorId),
                }
            );
            await db.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = _reviewsFactory.Services.CreateAsyncScope())
        {
            ReviewsDbContext db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
            db.ProductProjections.Add(
                new ProductProjection
                {
                    ProductId = productId,
                    TenantId = tenantId,
                    Name = "Test Product",
                    IsActive = true,
                }
            );
            db.ProductReviews.Add(
                new ProductReview
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    UserId = actorId,
                    Rating = 5,
                    Comment = "Great product",
                    TenantId = tenantId,
                    Audit = TestDataHelper.CreateAudit(actorId),
                }
            );
            await db.SaveChangesAsync();
        }

        // Act
        IHost productCatalogHost = _productCatalogFactory.Services.GetRequiredService<IHost>();
        IHost reviewsHost = _reviewsFactory.Services.GetRequiredService<IHost>();
        IHost fileStorageHost = _fileStorageFactory.Services.GetRequiredService<IHost>();

        ITrackedSession session = await productCatalogHost
            .TrackActivity()
            .Timeout(TestConstants.TrackedSessionTimeout)
            .IncludeExternalTransports()
            .AlsoTrack(reviewsHost)
            .AlsoTrack(fileStorageHost)
            .WaitForMessageToBeReceivedAt<ReviewsCascadeCompleted>(productCatalogHost)
            .WaitForMessageToBeReceivedAt<FilesCascadeCompleted>(productCatalogHost)
            .DoNotAssertOnExceptionsDetected()
            .InvokeMessageAndWaitAsync(
                new StartProductDeletionSaga(correlationId, [productId], tenantId, actorId)
            );

        // Assert
        session.Sent.MessagesOf<ProductDeletedIntegrationEvent>().ShouldNotBeEmpty();
        session.Received.MessagesOf<ReviewsCascadeCompleted>().ShouldNotBeEmpty();
        session.Received.MessagesOf<FilesCascadeCompleted>().ShouldNotBeEmpty();

        await using (AsyncServiceScope scope = _reviewsFactory.Services.CreateAsyncScope())
        {
            ReviewsDbContext db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();

            ProductProjection? projection = await db.ProductProjections.FirstOrDefaultAsync(p =>
                p.ProductId == productId
            );
            projection.ShouldNotBeNull();
            projection.IsActive.ShouldBeFalse();

            int deletedReviewCount = await db
                .ProductReviews.IgnoreQueryFilters()
                .CountAsync(r => r.ProductId == productId && r.IsDeleted);
            deletedReviewCount.ShouldBeGreaterThan(0);
        }
    }
}
