namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ.Coordination;

public interface IDistributedJobCoordinator
{
    Task ExecuteIfLeaderAsync(
        string jobName,
        Func<CancellationToken, Task> action,
        CancellationToken ct = default
    );
}
