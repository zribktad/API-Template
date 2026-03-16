using APITemplate.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ.Coordination;

public sealed class DragonflyDistributedJobCoordinator : IDistributedJobCoordinator
{
    private const int LeaseSeconds = 300;
    private static readonly LuaScript ReleaseLockScript = LuaScript.Prepare(
        "if redis.call('get', @key) == @value then return redis.call('del', @key) else return 0 end"
    );

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<DragonflyDistributedJobCoordinator> _logger;

    public DragonflyDistributedJobCoordinator(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<BackgroundJobsOptions> options,
        ILogger<DragonflyDistributedJobCoordinator> logger
    )
    {
        _connectionMultiplexer = connectionMultiplexer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteIfLeaderAsync(
        string jobName,
        Func<CancellationToken, Task> action,
        CancellationToken ct = default
    )
    {
        var database = await RequireCoordinationAsync(jobName);
        if (database is null)
        {
            await action(ct);
            return;
        }

        var lockKey = $"TickerQ:Leader:{jobName}";
        var lockValue = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

        var acquired = await database.StringSetAsync(
            lockKey,
            lockValue,
            TimeSpan.FromSeconds(LeaseSeconds),
            when: When.NotExists
        );

        if (!acquired)
        {
            _logger.LogDebug(
                "Skipped background job {JobName} because another instance currently owns the coordination lease.",
                jobName
            );
            return;
        }

        using var renewalCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var renewalTask = RenewLeaseAsync(database, lockKey, lockValue, renewalCts.Token);

        try
        {
            await action(ct);
        }
        finally
        {
            renewalCts.Cancel();
            await AwaitRenewalAsync(renewalTask);
            await ReleaseAsync(database, lockKey, lockValue);
        }
    }

    private Task<IDatabase?> RequireCoordinationAsync(string jobName)
    {
        if (!_connectionMultiplexer.IsConnected)
        {
            return Task.FromResult(
                HandleUnavailable(jobName, "DragonFly connection is not established.")
            );
        }

        try
        {
            return Task.FromResult<IDatabase?>(_connectionMultiplexer.GetDatabase());
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HandleUnavailable(jobName, "DragonFly coordination is unavailable.", ex)
            );
        }
    }

    private IDatabase? HandleUnavailable(
        string jobName,
        string message,
        Exception? innerException = null
    )
    {
        if (!_options.TickerQ.FailClosed)
        {
            _logger.LogWarning(
                innerException,
                "DragonFly coordination is unavailable for background job {JobName}; continuing because fail-closed is disabled. {Message}",
                jobName,
                message
            );
            return null;
        }

        throw CreateFailClosedException(jobName, message, innerException);
    }

    private InvalidOperationException CreateFailClosedException(
        string jobName,
        string message,
        Exception? innerException = null
    )
    {
        _logger.LogWarning(
            innerException,
            "Fail-closed coordination stopped background job {JobName}: {Message}",
            jobName,
            message
        );

        return new InvalidOperationException(
            $"Background job '{jobName}' did not start because DragonFly coordination is unavailable. {message}",
            innerException
        );
    }

    private static async Task AwaitRenewalAsync(Task renewalTask)
    {
        try
        {
            await renewalTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when the owner finishes and stops renewing the lease.
        }
    }

    private async Task RenewLeaseAsync(
        IDatabase database,
        string key,
        string value,
        CancellationToken ct
    )
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(LeaseSeconds / 3d));
        while (await timer.WaitForNextTickAsync(ct))
        {
            await database.ScriptEvaluateAsync(
                """
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('expire', KEYS[1], ARGV[2])
                end
                return 0
                """,
                keys: [key],
                values: [value, LeaseSeconds]
            );
        }
    }

    private static Task ReleaseAsync(IDatabase database, string key, string value) =>
        database.ScriptEvaluateAsync(ReleaseLockScript, new { key, value });
}
