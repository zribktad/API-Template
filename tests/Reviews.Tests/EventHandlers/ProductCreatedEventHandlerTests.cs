using Contracts.IntegrationEvents.ProductCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Reviews.Application.Features.ProductEvents;
using Reviews.Domain.Entities;
using Shouldly;
using Xunit;

namespace Reviews.Tests.EventHandlers;

public sealed class ProductCreatedEventHandlerTests
{
    private readonly Mock<ILogger<ProductCreatedEventHandler>> _loggerMock = new();

    [Fact]
    public async Task HandleAsync_WhenNewProduct_CreatesProjection()
    {
        using TestDbContext dbContext = CreateDbContext();
        Guid productId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        ProductCreatedIntegrationEvent @event = new(productId, tenantId, "Widget", DateTime.UtcNow);

        await ProductCreatedEventHandler.HandleAsync(
            @event,
            dbContext,
            _loggerMock.Object,
            CancellationToken.None
        );

        ProductProjection? projection = await dbContext.ProductProjections.FindAsync(productId);
        projection.ShouldNotBeNull();
        projection.Name.ShouldBe("Widget");
        projection.TenantId.ShouldBe(tenantId);
        projection.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenExistingProduct_ReactivatesAndUpdatesName()
    {
        using TestDbContext dbContext = CreateDbContext();
        Guid productId = Guid.NewGuid();
        Guid newTenantId = Guid.NewGuid();
        dbContext.ProductProjections.Add(
            new ProductProjection
            {
                ProductId = productId,
                TenantId = Guid.NewGuid(),
                Name = "Old Name",
                IsActive = false,
            }
        );
        await dbContext.SaveChangesAsync();

        ProductCreatedIntegrationEvent @event = new(
            productId,
            newTenantId,
            "New Name",
            DateTime.UtcNow
        );

        await ProductCreatedEventHandler.HandleAsync(
            @event,
            dbContext,
            _loggerMock.Object,
            CancellationToken.None
        );

        ProductProjection? projection = await dbContext.ProductProjections.FindAsync(productId);
        projection.ShouldNotBeNull();
        projection.Name.ShouldBe("New Name");
        projection.TenantId.ShouldBe(newTenantId);
        projection.IsActive.ShouldBeTrue();
    }

    private static TestDbContext CreateDbContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }
}
