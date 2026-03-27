using Contracts.IntegrationEvents.Identity;
using Contracts.IntegrationEvents.Sagas;
using Identity.Application.EventHandlers;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Wolverine;
using Xunit;

namespace Identity.Tests.EventHandlers;

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
    public async Task HandleAsync_SoftDeletesUsersForTenant()
    {
        Guid tenantId = Guid.NewGuid();
        Guid actorId = Guid.NewGuid();
        AppUser user1 = CreateUser(tenantId);
        AppUser user2 = CreateUser(tenantId);
        AppUser userOtherTenant = CreateUser(Guid.NewGuid());
        _dbContext.Users.AddRange(user1, user2, userOtherTenant);
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

        AppUser refreshed1 = await _dbContext
            .Users.IgnoreQueryFilters()
            .SingleAsync(u => u.Id == user1.Id);
        AppUser refreshed2 = await _dbContext
            .Users.IgnoreQueryFilters()
            .SingleAsync(u => u.Id == user2.Id);
        AppUser other = await _dbContext
            .Users.IgnoreQueryFilters()
            .SingleAsync(u => u.Id == userOtherTenant.Id);

        refreshed1.IsDeleted.ShouldBeTrue();
        refreshed1.DeletedBy.ShouldBe(actorId);
        refreshed2.IsDeleted.ShouldBeTrue();
        other.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_PublishesUsersCascadeCompletedWithCorrectCorrelationId()
    {
        Guid tenantId = Guid.NewGuid();
        Guid correlationId = Guid.NewGuid();
        _dbContext.Users.Add(CreateUser(tenantId));
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
                    It.Is<UsersCascadeCompleted>(m =>
                        m.TenantDeactivationSagaId == correlationId.ToString()
                        && m.TenantId == tenantId
                        && m.DeactivatedCount == 1
                    ),
                    It.IsAny<DeliveryOptions?>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_SkipsAlreadyDeletedUsers()
    {
        Guid tenantId = Guid.NewGuid();
        AppUser alreadyDeleted = CreateUser(tenantId);
        alreadyDeleted.IsDeleted = true;
        _dbContext.Users.Add(alreadyDeleted);
        await _dbContext.SaveChangesAsync();

        TenantDeactivatedIntegrationEvent @event = new(
            Guid.NewGuid(),
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
                    It.Is<UsersCascadeCompleted>(m => m.DeactivatedCount == 0),
                    It.IsAny<DeliveryOptions?>()
                ),
            Times.Once
        );
    }

    private static AppUser CreateUser(Guid tenantId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Username = "user-" + Guid.NewGuid().ToString("N")[..8],
            Email = $"user-{Guid.NewGuid():N}@test.com",
            TenantId = tenantId,
            Role = UserRole.User,
        };
}
