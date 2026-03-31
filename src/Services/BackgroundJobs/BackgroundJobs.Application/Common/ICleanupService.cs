namespace BackgroundJobs.Application.Common;

/// <summary>
/// Application-layer contract for scheduled data-cleanup operations.
/// Implementations live in the Infrastructure layer and are invoked by recurring background jobs.
/// </summary>
public interface ICleanupService
{
    /// <summary>
    /// Permanently purges soft-deleted job execution records that exceeded the <paramref name="retentionDays"/> retention window,
    /// processed in batches of <paramref name="batchSize"/>.
    /// </summary>
    Task CleanupSoftDeletedRecordsAsync(
        int retentionDays,
        int batchSize,
        CancellationToken ct = default
    );

    /// <summary>
    /// Removes completed or failed job executions older than <paramref name="retentionDays"/> days,
    /// processed in batches of <paramref name="batchSize"/>.
    /// </summary>
    Task CleanupStaleJobExecutionsAsync(
        int retentionDays,
        int batchSize,
        CancellationToken ct = default
    );
}
