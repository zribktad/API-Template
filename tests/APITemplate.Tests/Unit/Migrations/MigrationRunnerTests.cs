using APITemplate.Infrastructure.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Migrations;

public class MigrationRunnerTests
{
    private readonly Mock<IMongoDatabase> _databaseMock;
    private readonly Mock<IMongoCollection<MongoMigrationRecord>> _collectionMock;

    public MigrationRunnerTests()
    {
        _collectionMock = new Mock<IMongoCollection<MongoMigrationRecord>>();
        _databaseMock = new Mock<IMongoDatabase>();
        _databaseMock
            .Setup(d => d.GetCollection<MongoMigrationRecord>("_migrations", null))
            .Returns(_collectionMock.Object);
    }

    [Fact]
    public async Task RunAsync_NoPendingMigrations_DoesNotCallUpAsync()
    {
        var migration = new Mock<IMigration>();
        migration.Setup(m => m.Version).Returns(1);
        SetupAppliedMigrations([new MongoMigrationRecord { Version = 1 }]);

        var sut = CreateRunner([migration.Object]);
        await sut.RunAsync();

        migration.Verify(m => m.UpAsync(It.IsAny<IMongoDatabase>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_PendingMigration_CallsUpAsyncAndRecords()
    {
        var migration = new Mock<IMigration>();
        migration.Setup(m => m.Version).Returns(1);
        migration.Setup(m => m.Description).Returns("Test migration");
        SetupAppliedMigrations([]);

        var sut = CreateRunner([migration.Object]);
        await sut.RunAsync();

        migration.Verify(m => m.UpAsync(_databaseMock.Object, It.IsAny<CancellationToken>()), Times.Once);
        _collectionMock.Verify(
            c => c.InsertOneAsync(
                It.Is<MongoMigrationRecord>(r => r.Version == 1 && r.Description == "Test migration"),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_AlreadyAppliedMigration_IsSkipped()
    {
        var migration = new Mock<IMigration>();
        migration.Setup(m => m.Version).Returns(1);
        SetupAppliedMigrations([new MongoMigrationRecord { Version = 1 }]);

        var sut = CreateRunner([migration.Object]);
        await sut.RunAsync();

        _collectionMock.Verify(
            c => c.InsertOneAsync(It.IsAny<MongoMigrationRecord>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_MultiplesMigrations_OnlyRunsPending()
    {
        var migration1 = new Mock<IMigration>();
        migration1.Setup(m => m.Version).Returns(1);
        var migration2 = new Mock<IMigration>();
        migration2.Setup(m => m.Version).Returns(2);
        migration2.Setup(m => m.Description).Returns("Migration 2");

        SetupAppliedMigrations([new MongoMigrationRecord { Version = 1 }]);

        var sut = CreateRunner([migration1.Object, migration2.Object]);
        await sut.RunAsync();

        migration1.Verify(m => m.UpAsync(It.IsAny<IMongoDatabase>(), It.IsAny<CancellationToken>()), Times.Never);
        migration2.Verify(m => m.UpAsync(_databaseMock.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_MultiplePending_RunsInVersionOrder()
    {
        var executionOrder = new List<int>();

        var migration2 = new Mock<IMigration>();
        migration2.Setup(m => m.Version).Returns(2);
        migration2.Setup(m => m.UpAsync(It.IsAny<IMongoDatabase>(), It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add(2))
            .Returns(Task.CompletedTask);

        var migration1 = new Mock<IMigration>();
        migration1.Setup(m => m.Version).Returns(1);
        migration1.Setup(m => m.UpAsync(It.IsAny<IMongoDatabase>(), It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add(1))
            .Returns(Task.CompletedTask);

        SetupAppliedMigrations([]);

        // Intentionally pass out of order
        var sut = CreateRunner([migration2.Object, migration1.Object]);
        await sut.RunAsync();

        executionOrder.ShouldBe([1, 2]);
    }

    [Fact]
    public async Task RunAsync_EmptyMigrationList_DoesNothing()
    {
        SetupAppliedMigrations([]);

        var sut = CreateRunner([]);
        await sut.RunAsync();

        _collectionMock.Verify(
            c => c.InsertOneAsync(It.IsAny<MongoMigrationRecord>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private MigrationRunner CreateRunner(IReadOnlyList<IMigration> migrations) =>
        new(_databaseMock.Object, NullLogger<MigrationRunner>.Instance, migrations);

    private void SetupAppliedMigrations(List<MongoMigrationRecord> records)
    {
        var cursorMock = new Mock<IAsyncCursor<MongoMigrationRecord>>();
        cursorMock.Setup(c => c.Current).Returns(records);
        cursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(records.Count > 0)
            .ReturnsAsync(false);

        _collectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<MongoMigrationRecord>>(),
                It.IsAny<FindOptions<MongoMigrationRecord, MongoMigrationRecord>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);
    }
}
