using System.ComponentModel.DataAnnotations;

namespace BackgroundJobs.Application.Options;

/// <summary>
/// Configuration for the periodic cleanup job that purges soft-deleted records
/// and stale job executions according to the configured retention windows.
/// </summary>
public sealed class CleanupJobOptions
{
    public bool Enabled { get; init; }

    [Required]
    public string Cron { get; init; } = "0 * * * *";

    [Range(1, int.MaxValue)]
    public int SoftDeleteRetentionDays { get; init; } = 30;

    [Range(1, int.MaxValue)]
    public int StaleJobRetentionDays { get; init; } = 90;

    [Range(1, int.MaxValue)]
    public int BatchSize { get; init; } = 100;
}
