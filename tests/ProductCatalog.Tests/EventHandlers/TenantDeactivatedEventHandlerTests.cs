using Contracts.IntegrationEvents.Identity;
using Contracts.IntegrationEvents.Sagas;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Application.EventHandlers;
using ProductCatalog.Domain.Entities;
using Shouldly;
using Wolverine;
using Xunit;

namespace ProductCatalog.Tests.EventHandlers;

public sealed class TenantDeactivatedEventHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IMessageBus> _busMock = new();
    private readonly Mock<ILogger<TenantDeactivatedEventHandler>> _loggerMock = new();

    public TenantDeactivatedEventHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task HandleAsync_SoftDeletesProductsForTenant()
    {
        Guid tenantId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Product p1 = CreateProduct(tenantId);
        Product p2 = CreateProduct(tenantId);
        Product pOther = CreateProduct(Guid.NewGuid());
        _dbContext.Products.AddRange(p1, p2, pOther);
        await _dbContext.SaveChangesAsync();

        TenantDeactivatedIntegrationEvent @event = new(
            Guid.NewGuid(),
            tenantId,
            actorId,
            DateTime.UtcNow
        );

        await TenantDeactivatedEventHandler.HandleAsync(
            @event,
            _dbContext,
            _busMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            CancellationToken.None
        );

        Product rp1 = await _dbContext
            .Products.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == p1.Id);
        Product rp2 = await _dbContext
            .Products.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == p2.Id);
        Product other = await _dbContext
            .Products.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == pOther.Id);

        rp1.IsDeleted.ShouldBeTrue();
        rp1.DeletedBy.ShouldBe(actorId);
        rp2.IsDeleted.ShouldBeTrue();
        other.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_SoftDeletesCategoriesForTenant()
    {
        Guid tenantId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        Category c1 = CreateCategory(tenantId);
        Category cOther = CreateCategory(Guid.NewGuid());
        _dbContext.Categories.AddRange(c1, cOther);
        await _dbContext.SaveChangesAsync();

        TenantDeactivatedIntegrationEvent @event = new(
            Guid.NewGuid(),
            tenantId,
            actorId,
            DateTime.UtcNow
        );

        await TenantDeactivatedEventHandler.HandleAsync(
            @event,
            _dbContext,
            _busMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            CancellationToken.None
        );

        Category rc1 = await _dbContext
            .Categories.IgnoreQueryFilters()
            .SingleAsync(c => c.Id == c1.Id);
        Category other = await _dbContext
            .Categories.IgnoreQueryFilters()
            .SingleAsync(c => c.Id == cOther.Id);

        rc1.IsDeleted.ShouldBeTrue();
        rc1.DeletedBy.ShouldBe(actorId);
        other.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_PublishesProductsCascadeCompletedWithCorrectCorrelationId()
    {
        Guid tenantId = Guid.NewGuid();
        Guid correlationId = Guid.NewGuid();
        _dbContext.Products.Add(CreateProduct(tenantId));
        await _dbContext.SaveChangesAsync();

        TenantDeactivatedIntegrationEvent @event = new(
            correlationId,
            tenantId,
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        await TenantDeactivatedEventHandler.HandleAsync(
            @event,
            _dbContext,
            _busMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            CancellationToken.None
        );

        _busMock.Verify(
            b =>
                b.PublishAsync(
                    It.Is<ProductsCascadeCompleted>(m =>
                        m.TenantDeactivationSagaId == correlationId.ToString()
                        && m.TenantId == tenantId
                        && m.DeletedCount == 1
                    ),
                    It.IsAny<DeliveryOptions?>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_PublishesCategoriesCascadeCompletedWithCorrectCorrelationId()
    {
        Guid tenantId = Guid.NewGuid();
        Guid correlationId = Guid.NewGuid();
        _dbContext.Categories.Add(CreateCategory(tenantId));
        await _dbContext.SaveChangesAsync();

        TenantDeactivatedIntegrationEvent @event = new(
            correlationId,
            tenantId,
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        await TenantDeactivatedEventHandler.HandleAsync(
            @event,
            _dbContext,
            _busMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            CancellationToken.None
        );

        _busMock.Verify(
            b =>
                b.PublishAsync(
                    It.Is<CategoriesCascadeCompleted>(m =>
                        m.TenantDeactivationSagaId == correlationId.ToString()
                        && m.TenantId == tenantId
                        && m.DeletedCount == 1
                    ),
                    It.IsAny<DeliveryOptions?>()
                ),
            Times.Once
        );
    }

    private static Product CreateProduct(Guid tenantId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Product-" + Guid.NewGuid().ToString("N")[..8],
            Price = 9.99m,
            TenantId = tenantId,
        };

    private static Category CreateCategory(Guid tenantId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Category-" + Guid.NewGuid().ToString("N")[..8],
            TenantId = tenantId,
        };
}
