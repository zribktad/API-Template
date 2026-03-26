using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Application.Options;
using SharedKernel.Domain.Interfaces;
using SharedKernel.Domain.Options;

namespace SharedKernel.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/> backed by <see cref="DbContext"/>.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private const string CommitWithinTransactionMessage =
        "CommitAsync cannot be called inside ExecuteInTransactionAsync. The outermost transaction saves and commits automatically.";

    private readonly DbContext _dbContext;
    private readonly TransactionDefaultsOptions _transactionDefaults;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly IDbTransactionProvider _transactionProvider;
    private readonly ManagedTransactionScope _managedTransactionScope = new();
    private readonly DbContextTrackedStateManager _trackedStateManager;
    private readonly DbContextCommandTimeoutScope _commandTimeoutScope;
    private int _savepointCounter;
    private TransactionOptions? _activeTransactionOptions;

    /// <summary>
    /// Creates a <see cref="UnitOfWork"/> that uses configured transaction defaults for explicit transactions.
    /// </summary>
    /// <param name="dbContext">EF Core context that tracks staged relational changes for the current scope.</param>
    /// <param name="transactionDefaults">
    /// Configured defaults used to resolve the effective isolation level, timeout, and retry policy
    /// for outermost <see cref="ExecuteInTransactionAsync"/> calls.
    /// </param>
    /// <param name="logger">Logger used for transaction orchestration diagnostics.</param>
    /// <param name="transactionProvider">Provides transaction management operations for the underlying database.</param>
    public UnitOfWork(
        DbContext dbContext,
        IOptions<TransactionDefaultsOptions> transactionDefaults,
        ILogger<UnitOfWork> logger,
        IDbTransactionProvider transactionProvider
    )
    {
        _dbContext = dbContext;
        _transactionDefaults = transactionDefaults.Value;
        _logger = logger;
        _transactionProvider = transactionProvider;
        _trackedStateManager = new DbContextTrackedStateManager(dbContext);
        _commandTimeoutScope = new DbContextCommandTimeoutScope(dbContext);
    }

    /// <summary>
    /// Persists all currently staged relational changes without opening an explicit transaction boundary.
    /// Use this for simple service flows that already know when the write should be flushed.
    /// Retries are managed by this unit of work using the configured default transaction policy.
    /// </summary>
    /// <param name="ct">Cancellation token for the underlying <c>SaveChangesAsync</c> call.</param>
    /// <returns>A task that completes when all staged changes have been flushed to the database.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called inside <see cref="ExecuteInTransactionAsync"/> because the outermost managed transaction
    /// owns the save and commit lifecycle.
    /// </exception>
    public Task CommitAsync(CancellationToken ct = default)
    {
        if (_managedTransactionScope.IsActive)
        {
            _logger.CommitRejectedInsideManagedTransaction();
            throw new InvalidOperationException(CommitWithinTransactionMessage);
        }

        TransactionOptions effectiveOptions = _transactionDefaults.Resolve(null);
        _logger.CommitStarted(
            effectiveOptions.RetryEnabled ?? true,
            effectiveOptions.TimeoutSeconds
        );
        IExecutionStrategy strategy = _transactionProvider.CreateExecutionStrategy(
            effectiveOptions
        );
        return strategy.ExecuteAsync(
            async cancellationToken =>
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.CommitCompleted();
            },
            ct
        );
    }

    /// <summary>
    /// Executes a write delegate inside an explicit relational transaction.
    /// The outermost call owns transaction creation, retry strategy, timeout application, save, and commit.
    /// </summary>
    /// <param name="action">
    /// Delegate that stages repository/entity changes inside the transaction boundary.
    /// The delegate should not call <see cref="CommitAsync"/>.
    /// </param>
    /// <param name="ct">Cancellation token propagated to transaction, savepoint, and save operations.</param>
    /// <param name="options">
    /// Optional per-call transaction overrides. Non-null values override configured defaults only for the outermost call.
    /// Nested calls inherit the already active outer transaction policy.
    /// </param>
    /// <returns>A task that completes when the transactional delegate has been saved and committed.</returns>
    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken ct = default,
        TransactionOptions? options = null
    ) =>
        await ExecuteInTransactionAsync(
            async () =>
            {
                await action();
                return true;
            },
            ct,
            options
        );

    /// <summary>
    /// Executes a write delegate inside an explicit relational transaction and returns a value created by that flow.
    /// Per-call <paramref name="options"/> override configured defaults only for the outermost transaction boundary.
    /// </summary>
    /// <typeparam name="T">Type returned by the transactional delegate.</typeparam>
    /// <param name="action">
    /// Delegate that stages repository/entity changes and returns a value computed inside the transaction boundary.
    /// </param>
    /// <param name="ct">Cancellation token propagated to transaction, savepoint, and save operations.</param>
    /// <param name="options">
    /// Optional per-call transaction overrides. Non-null values override configured defaults only for the outermost call.
    /// Nested calls inherit the already active outer transaction policy.
    /// </param>
    /// <returns>The value returned by <paramref name="action"/> after the transaction has been saved and committed.</returns>
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken ct = default,
        TransactionOptions? options = null
    )
    {
        IDbContextTransaction? currentTransaction = _transactionProvider.CurrentTransaction;
        if (currentTransaction is not null)
            return await ExecuteWithinSavepointAsync(currentTransaction, action, options, ct);

        TransactionOptions effectiveOptions = _transactionDefaults.Resolve(options);
        return await ExecuteAsOutermostTransactionAsync(action, effectiveOptions, ct);
    }

    /// <summary>
    /// Executes a nested transaction scope by using a savepoint inside the active outer transaction.
    /// </summary>
    private async Task<T> ExecuteWithinSavepointAsync<T>(
        IDbContextTransaction transaction,
        Func<Task<T>> action,
        TransactionOptions? options,
        CancellationToken ct
    )
    {
        ValidateNestedTransactionOptions(options);
        string savepointName = $"uow_sp_{Interlocked.Increment(ref _savepointCounter)}";
        IReadOnlyDictionary<object, DbContextTrackedStateManager.TrackedEntitySnapshot> snapshot =
            _trackedStateManager.Capture();

        _logger.SavepointCreating(savepointName);
        await transaction.CreateSavepointAsync(savepointName, ct);
        try
        {
            using IDisposable scope = _managedTransactionScope.Enter();
            T result = await action();
            await ReleaseSavepointIfSupportedAsync(transaction, savepointName, ct);
            _logger.SavepointReleased(savepointName);
            return result;
        }
        catch
        {
            await transaction.RollbackToSavepointAsync(savepointName, ct);
            _trackedStateManager.Restore(snapshot);
            _logger.SavepointRolledBack(savepointName);
            throw;
        }
    }

    /// <summary>
    /// Executes the outermost transaction boundary through EF Core's execution strategy so the whole unit
    /// of work can be replayed on transient relational failures.
    /// </summary>
    private async Task<T> ExecuteAsOutermostTransactionAsync<T>(
        Func<Task<T>> action,
        TransactionOptions effectiveOptions,
        CancellationToken ct
    )
    {
        IExecutionStrategy strategy = _transactionProvider.CreateExecutionStrategy(
            effectiveOptions
        );
        TransactionOptions? previousActiveOptions = _activeTransactionOptions;

        return await strategy.ExecuteAsync(
            state: action,
            operation: async (_, transactionalAction, cancellationToken) =>
            {
                _activeTransactionOptions = effectiveOptions;
                using IDisposable timeoutScope = _commandTimeoutScope.Apply(
                    effectiveOptions.TimeoutSeconds
                );
                _logger.OutermostTransactionStarted(
                    effectiveOptions.IsolationLevel!.Value,
                    effectiveOptions.TimeoutSeconds,
                    effectiveOptions.RetryEnabled ?? true
                );

                IDbContextTransaction? transaction = null;
                try
                {
                    transaction = await _transactionProvider.BeginTransactionAsync(
                        effectiveOptions.IsolationLevel!.Value,
                        cancellationToken
                    );
                    _logger.DatabaseTransactionOpened();
                }
                catch (Exception ex) when (IsTransactionNotSupported(ex))
                {
                    _logger.DatabaseTransactionUnsupported(ex);
                }

                IReadOnlyDictionary<
                    object,
                    DbContextTrackedStateManager.TrackedEntitySnapshot
                > snapshot = _trackedStateManager.Capture();

                try
                {
                    using IDisposable scope = _managedTransactionScope.Enter();
                    T result = await transactionalAction();
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                        _logger.DatabaseTransactionCommitted();
                    }

                    _logger.OutermostTransactionCompleted();
                    return result;
                }
                catch (Exception ex)
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.DatabaseTransactionRolledBack(ex);
                    }

                    _trackedStateManager.Restore(snapshot);
                    throw;
                }
                finally
                {
                    if (transaction is not null)
                        await transaction.DisposeAsync();

                    _activeTransactionOptions = previousActiveOptions;
                }
            },
            verifySucceeded: null,
            ct
        );
    }

    /// <summary>
    /// Ensures nested transaction scopes inherit the effective outer transaction policy.
    /// </summary>
    private void ValidateNestedTransactionOptions(TransactionOptions? options)
    {
        if (_activeTransactionOptions is null)
            throw new InvalidOperationException(
                "Nested transaction execution requires an active outer transaction policy."
            );

        if (options is null || options.IsEmpty())
            return;

        TransactionOptions effectiveOptions = _transactionDefaults.Resolve(options);
        if (effectiveOptions != _activeTransactionOptions)
        {
            throw new InvalidOperationException(
                "Nested transactions inherit the active outer transaction options. "
                    + "Pass null/default options inside nested ExecuteInTransactionAsync calls."
            );
        }
    }

    /// <summary>
    /// Releases the current savepoint when the provider supports explicit savepoint release.
    /// </summary>
    private async Task ReleaseSavepointIfSupportedAsync(
        IDbContextTransaction transaction,
        string savepointName,
        CancellationToken ct
    )
    {
        try
        {
            await transaction.ReleaseSavepointAsync(savepointName, ct);
        }
        catch (NotSupportedException) { }
    }

    private static bool IsTransactionNotSupported(Exception ex) =>
        ex is InvalidOperationException or NotSupportedException;
}
