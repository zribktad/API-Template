using System.Data;
using APITemplate.Application.Common.Options;
using APITemplate.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace APITemplate.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly TransactionDefaultsOptions _transactionDefaults;
    private readonly Func<TransactionOptions, IExecutionStrategy> _executionStrategyFactory;
    private readonly Func<IDbContextTransaction?> _currentTransactionAccessor;
    private readonly Func<IsolationLevel, CancellationToken, Task<IDbContextTransaction>> _beginTransactionAsync;
    private int _savepointCounter;
    private TransactionOptions? _activeTransactionOptions;

    public UnitOfWork(AppDbContext dbContext)
        : this(dbContext, Options.Create(new TransactionDefaultsOptions()))
    {
    }

    public UnitOfWork(AppDbContext dbContext, IOptions<TransactionDefaultsOptions> transactionDefaults)
        : this(
            dbContext,
            transactionDefaults.Value,
            options => CreateExecutionStrategy(dbContext, options),
            () => dbContext.Database.CurrentTransaction,
            dbContext.Database.BeginTransactionAsync)
    {
    }

    internal UnitOfWork(
        AppDbContext dbContext,
        TransactionDefaultsOptions transactionDefaults,
        Func<TransactionOptions, IExecutionStrategy> executionStrategyFactory,
        Func<IDbContextTransaction?> currentTransactionAccessor,
        Func<IsolationLevel, CancellationToken, Task<IDbContextTransaction>> beginTransactionAsync)
    {
        _dbContext = dbContext;
        _transactionDefaults = transactionDefaults;
        _executionStrategyFactory = executionStrategyFactory;
        _currentTransactionAccessor = currentTransactionAccessor;
        _beginTransactionAsync = beginTransactionAsync;
    }

    public Task CommitAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken ct = default,
        TransactionOptions? options = null)
        => await ExecuteInTransactionAsync(
            async () =>
            {
                await action();
                return true;
            },
            ct,
            options);

    // For multi-step write flows only. The outermost call owns the transaction and retries; nested calls use savepoints.
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken ct = default,
        TransactionOptions? options = null)
    {
        var currentTransaction = _currentTransactionAccessor();
        if (currentTransaction is not null)
            return await ExecuteWithinSavepointAsync(currentTransaction, action, options, ct);

        var effectiveOptions = _transactionDefaults.Resolve(options);
        var strategy = _executionStrategyFactory(effectiveOptions);
        var previousActiveOptions = _activeTransactionOptions;
        var previousTimeout = _dbContext.Database.GetCommandTimeout();
        return await strategy.ExecuteAsync(
            state: action,
            operation: async (_, transactionalAction, cancellationToken) =>
            {
                _activeTransactionOptions = effectiveOptions;
                _dbContext.Database.SetCommandTimeout(effectiveOptions.TimeoutSeconds);

                await using var transaction = await _beginTransactionAsync(
                    effectiveOptions.IsolationLevel!.Value,
                    cancellationToken);
                var snapshot = CaptureTrackedState();
                try
                {
                    var result = await transactionalAction();
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    RestoreTrackedState(snapshot);
                    throw;
                }
                finally
                {
                    _dbContext.Database.SetCommandTimeout(previousTimeout);
                    _activeTransactionOptions = previousActiveOptions;
                }
            },
            verifySucceeded: null,
            ct);
    }

    private async Task<T> ExecuteWithinSavepointAsync<T>(
        IDbContextTransaction transaction,
        Func<Task<T>> action,
        TransactionOptions? options,
        CancellationToken ct)
    {
        if (_activeTransactionOptions is null)
            throw new InvalidOperationException("Nested transaction execution requires an active outer transaction policy.");

        if (options is not null && !options.IsEmpty())
        {
            var effectiveOptions = _transactionDefaults.Resolve(options);
            if (effectiveOptions != _activeTransactionOptions)
            {
                throw new InvalidOperationException(
                    "Nested transactions inherit the active outer transaction options. " +
                    "Pass null/default options inside nested ExecuteInTransactionAsync calls.");
            }
        }

        var savepointName = $"uow_sp_{Interlocked.Increment(ref _savepointCounter)}";
        var snapshot = CaptureTrackedState();

        await transaction.CreateSavepointAsync(savepointName, ct);
        try
        {
            var result = await action();
            await ReleaseSavepointIfSupportedAsync(transaction, savepointName, ct);
            return result;
        }
        catch
        {
            await transaction.RollbackToSavepointAsync(savepointName, ct);
            RestoreTrackedState(snapshot);
            throw;
        }
    }

    private async Task ReleaseSavepointIfSupportedAsync(
        IDbContextTransaction transaction,
        string savepointName,
        CancellationToken ct)
    {
        try
        {
            await transaction.ReleaseSavepointAsync(savepointName, ct);
        }
        catch (NotSupportedException)
        {
        }
    }

    private IReadOnlyDictionary<object, TrackedEntitySnapshot> CaptureTrackedState()
    {
        return _dbContext.ChangeTracker
            .Entries()
            .Where(entry => entry.State != EntityState.Detached)
            .ToDictionary(
                entry => entry.Entity,
                entry => new TrackedEntitySnapshot(
                    entry.State,
                    entry.CurrentValues.Clone(),
                    entry.OriginalValues.Clone()),
                ReferenceEqualityComparer.Instance);
    }

    private void RestoreTrackedState(IReadOnlyDictionary<object, TrackedEntitySnapshot> snapshot)
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
        {
            if (!snapshot.TryGetValue(entry.Entity, out var entitySnapshot))
            {
                entry.State = EntityState.Detached;
                continue;
            }

            entry.CurrentValues.SetValues(entitySnapshot.CurrentValues);
            entry.OriginalValues.SetValues(entitySnapshot.OriginalValues);
            entry.State = entitySnapshot.State;
        }
    }

    private sealed record TrackedEntitySnapshot(
        EntityState State,
        PropertyValues CurrentValues,
        PropertyValues OriginalValues);

    private static IExecutionStrategy CreateExecutionStrategy(
        DbContext dbContext,
        TransactionOptions effectiveOptions)
    {
        if (effectiveOptions.RetryEnabled == false)
            return new NonRetryingExecutionStrategy(dbContext);

        return new NpgsqlRetryingExecutionStrategy(
            dbContext,
            effectiveOptions.RetryCount ?? 3,
            TimeSpan.FromSeconds(effectiveOptions.RetryDelaySeconds ?? 5),
            errorCodesToAdd: null);
    }
}
