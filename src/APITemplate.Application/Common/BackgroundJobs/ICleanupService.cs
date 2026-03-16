namespace APITemplate.Application.Common.BackgroundJobs;

public interface ICleanupService
{
    Task CleanupExpiredInvitationsAsync(
        int retentionHours,
        int batchSize,
        CancellationToken ct = default
    );
    Task CleanupSoftDeletedRecordsAsync(
        int retentionDays,
        int batchSize,
        CancellationToken ct = default
    );
    Task CleanupOrphanedProductDataAsync(
        int retentionDays,
        int batchSize,
        CancellationToken ct = default
    );
}
