using APITemplate.Application.Common.Context;
using APITemplate.Domain.Entities;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Persistence;

public class UnitOfWorkTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_UsesExecutionStrategy_AndPersistsChanges()
    {
        var executionStrategy = new RecordingExecutionStrategy();
        await using var dbContext = CreateDbContext();
        IDbContextTransaction? currentTransaction = null;
        var sut = new UnitOfWork(
            dbContext,
            () => executionStrategy,
            () => currentTransaction,
            _ =>
            {
                currentTransaction = new RecordingTransaction();
                return Task.FromResult(currentTransaction);
            });

        await sut.ExecuteInTransactionAsync(async () =>
        {
            dbContext.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = "Books"
            });

            await Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        executionStrategy.ExecuteAsyncCallCount.ShouldBe(1);
        (await dbContext.Categories.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncOfT_UsesExecutionStrategy_AndReturnsResult()
    {
        var executionStrategy = new RecordingExecutionStrategy();
        await using var dbContext = CreateDbContext();
        IDbContextTransaction? currentTransaction = null;
        var sut = new UnitOfWork(
            dbContext,
            () => executionStrategy,
            () => currentTransaction,
            _ =>
            {
                currentTransaction = new RecordingTransaction();
                return Task.FromResult(currentTransaction);
            });

        var createdId = await sut.ExecuteInTransactionAsync(async () =>
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Games"
            };

            dbContext.Categories.Add(category);
            await Task.CompletedTask;
            return category.Id;
        }, TestContext.Current.CancellationToken);

        executionStrategy.ExecuteAsyncCallCount.ShouldBe(1);
        (await dbContext.Categories.SingleAsync(c => c.Id == createdId, TestContext.Current.CancellationToken)).Name.ShouldBe("Games");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenActionThrows_RollsBackAndPropagates()
    {
        var executionStrategy = new RecordingExecutionStrategy();
        await using var dbContext = CreateDbContext();
        IDbContextTransaction? currentTransaction = null;
        var transaction = new RecordingTransaction();
        var sut = new UnitOfWork(
            dbContext,
            () => executionStrategy,
            () => currentTransaction,
            _ =>
            {
                currentTransaction = transaction;
                return Task.FromResult<IDbContextTransaction>(transaction);
            });

        var act = () => sut.ExecuteInTransactionAsync(async () =>
        {
            dbContext.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = "Music"
            });

            await Task.CompletedTask;
            throw new InvalidOperationException("boom");
        }, TestContext.Current.CancellationToken);

        await Should.ThrowAsync<InvalidOperationException>(act);
        executionStrategy.ExecuteAsyncCallCount.ShouldBe(1);
        transaction.RollbackCount.ShouldBe(1);
        (await dbContext.Categories.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenNestedFailureIsCaught_RollsBackToSavepoint_AndPersistsOuterChanges()
    {
        var executionStrategy = new RecordingExecutionStrategy();
        await using var dbContext = CreateDbContext();
        IDbContextTransaction? currentTransaction = null;
        var transaction = new RecordingTransaction();
        var sut = new UnitOfWork(
            dbContext,
            () => executionStrategy,
            () => currentTransaction,
            _ =>
            {
                currentTransaction = transaction;
                return Task.FromResult<IDbContextTransaction>(transaction);
            });

        await sut.ExecuteInTransactionAsync(async () =>
        {
            dbContext.Categories.Add(new Category { Id = Guid.NewGuid(), Name = "Outer-A" });

            try
            {
                await sut.ExecuteInTransactionAsync(async () =>
                {
                    dbContext.Categories.Add(new Category { Id = Guid.NewGuid(), Name = "Inner" });
                    await Task.CompletedTask;
                    throw new InvalidOperationException("inner failure");
                }, TestContext.Current.CancellationToken);
            }
            catch (InvalidOperationException)
            {
            }

            dbContext.Categories.Add(new Category { Id = Guid.NewGuid(), Name = "Outer-B" });
        }, TestContext.Current.CancellationToken);

        executionStrategy.ExecuteAsyncCallCount.ShouldBe(1);
        transaction.BeginCount.ShouldBe(1);
        transaction.CreateSavepointCount.ShouldBe(1);
        transaction.RollbackToSavepointCount.ShouldBe(1);
        transaction.CommitCount.ShouldBe(1);

        var categoryNames = await dbContext.Categories
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync(TestContext.Current.CancellationToken);

        categoryNames.ShouldBe(["Outer-A", "Outer-B"]);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenNestedGenericSucceeds_UsesSavepoint_AndReturnsValue()
    {
        var executionStrategy = new RecordingExecutionStrategy();
        await using var dbContext = CreateDbContext();
        IDbContextTransaction? currentTransaction = null;
        var transaction = new RecordingTransaction();
        var sut = new UnitOfWork(
            dbContext,
            () => executionStrategy,
            () => currentTransaction,
            _ =>
            {
                currentTransaction = transaction;
                return Task.FromResult<IDbContextTransaction>(transaction);
            });

        var nestedResult = await sut.ExecuteInTransactionAsync(async () =>
        {
            return await sut.ExecuteInTransactionAsync(async () =>
            {
                var category = new Category { Id = Guid.NewGuid(), Name = "Nested" };
                dbContext.Categories.Add(category);
                await Task.CompletedTask;
                return category.Name;
            }, TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        nestedResult.ShouldBe("Nested");
        executionStrategy.ExecuteAsyncCallCount.ShouldBe(1);
        transaction.CreateSavepointCount.ShouldBe(1);
        transaction.ReleaseSavepointCount.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenNestedFailureBubbles_RollsBackOuterTransaction()
    {
        var executionStrategy = new RecordingExecutionStrategy();
        await using var dbContext = CreateDbContext();
        IDbContextTransaction? currentTransaction = null;
        var transaction = new RecordingTransaction();
        var sut = new UnitOfWork(
            dbContext,
            () => executionStrategy,
            () => currentTransaction,
            _ =>
            {
                currentTransaction = transaction;
                return Task.FromResult<IDbContextTransaction>(transaction);
            });

        var act = () => sut.ExecuteInTransactionAsync(async () =>
        {
            dbContext.Categories.Add(new Category { Id = Guid.NewGuid(), Name = "Outer" });

            await sut.ExecuteInTransactionAsync(async () =>
            {
                dbContext.Categories.Add(new Category { Id = Guid.NewGuid(), Name = "Inner" });
                await Task.CompletedTask;
                throw new InvalidOperationException("nested bubble");
            }, TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        await Should.ThrowAsync<InvalidOperationException>(act);
        executionStrategy.ExecuteAsyncCallCount.ShouldBe(1);
        transaction.CreateSavepointCount.ShouldBe(1);
        transaction.RollbackToSavepointCount.ShouldBe(1);
        transaction.RollbackCount.ShouldBe(1);
        (await dbContext.Categories.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, new TestTenantProvider(), new TestActorProvider());
    }

    private sealed class TestTenantProvider : ITenantProvider
    {
        public Guid TenantId => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public bool HasTenant => true;
    }

    private sealed class TestActorProvider : IActorProvider
    {
        public Guid ActorId => Guid.Empty;
    }

    private sealed class RecordingExecutionStrategy : IExecutionStrategy
    {
        public int ExecuteAsyncCallCount { get; private set; }
        public bool RetriesOnFailure => true;

        public void Execute(Action operation)
            => throw new NotSupportedException();

        public TResult Execute<TResult>(Func<TResult> operation)
            => throw new NotSupportedException();

        public TResult Execute<TState, TResult>(
            TState state,
            Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
            => throw new NotSupportedException();

        public Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            ExecuteAsyncCallCount++;
            return operation(cancellationToken);
        }

        public Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            ExecuteAsyncCallCount++;
            return operation(cancellationToken);
        }

        public Task<TResult> ExecuteAsync<TState, TResult>(
            TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            CancellationToken cancellationToken = default)
        {
            ExecuteAsyncCallCount++;
            return operation(null!, state, cancellationToken);
        }
    }

    private sealed class RecordingTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public int BeginCount { get; set; } = 1;
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }
        public int CreateSavepointCount { get; private set; }
        public int RollbackToSavepointCount { get; private set; }
        public int ReleaseSavepointCount { get; private set; }

        public void Commit() => CommitCount++;
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCount++;
            return Task.CompletedTask;
        }

        public void Rollback() => RollbackCount++;
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCount++;
            return Task.CompletedTask;
        }

        public void CreateSavepoint(string name) => CreateSavepointCount++;
        public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            CreateSavepointCount++;
            return Task.CompletedTask;
        }

        public void RollbackToSavepoint(string name) => RollbackToSavepointCount++;
        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            RollbackToSavepointCount++;
            return Task.CompletedTask;
        }

        public void ReleaseSavepoint(string name) => ReleaseSavepointCount++;
        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ReleaseSavepointCount++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }
}
