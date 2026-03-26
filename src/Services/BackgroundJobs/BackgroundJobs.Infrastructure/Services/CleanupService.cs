using BackgroundJobs.Application.Common;
using BackgroundJobs.Domain.Entities;
using BackgroundJobs.Domain.Enums;
using BackgroundJobs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackgroundJobs.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ICleanupService"/> that performs
/// scheduled data-hygiene tasks: soft-deleted records and stale job executions.
/// </summary>
public sealed class CleanupService : ICleanupService
{
    private readonly BackgroundJobsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(
        BackgroundJobsDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<CleanupService> logger
    )
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Permanently deletes soft-deleted job execution records older than <paramref name="retentionDays"/> days,
    /// processed in batches of <paramref name="batchSize"/>.
    /// </summary>
    public async Task CleanupSoftDeletedRecordsAsync(
        int retentionDays,
        int batchSize,
        CancellationToken ct = default
    )
    {
        DateTime cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-retentionDays);
        int totalDeleted = 0;
        int deleted;

        do
        {
            deleted = await _dbContext
                .JobExecutions.IgnoreQueryFilters()
                .Where(e => e.IsDeleted && e.DeletedAtUtc < cutoff)
                .OrderBy(e => e.DeletedAtUtc)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);

            totalDeleted += deleted;
        } while (deleted == batchSize);

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} soft-deleted job execution records.",
                totalDeleted
            );
        }
    }

    /// <summary>
    /// Removes completed or failed job executions older than <paramref name="retentionDays"/> days,
    /// processed in batches of <paramref name="batchSize"/>.
    /// </summary>
    public async Task CleanupStaleJobExecutionsAsync(
        int retentionDays,
        int batchSize,
        CancellationToken ct = default
    )
    {
        DateTime cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-retentionDays);
        int totalDeleted = 0;
        int deleted;

        do
        {
            deleted = await _dbContext
                .JobExecutions.Where(e =>
                    (e.Status == JobStatus.Completed || e.Status == JobStatus.Failed)
                    && e.CompletedAtUtc < cutoff
                )
                .OrderBy(e => e.CompletedAtUtc)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);

            totalDeleted += deleted;
        } while (deleted == batchSize);

        if (totalDeleted > 0)
        {
            _logger.LogInformation("Cleaned up {Count} stale job execution records.", totalDeleted);
        }
    }
}
