namespace APITemplate.Infrastructure.BackgroundJobs.Services;

public interface ISoftDeleteCleanupStrategy
{
    string EntityName { get; }
    Task<int> CleanupAsync(DateTime cutoff, int batchSize, CancellationToken ct = default);
}
