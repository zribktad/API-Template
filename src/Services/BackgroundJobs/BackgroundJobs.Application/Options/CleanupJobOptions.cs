namespace BackgroundJobs.Application.Options;

/// <summary>
/// Configuration for the periodic cleanup job that purges soft-deleted records
/// and stale job executions according to the configured retention windows.
/// </summary>
public sealed class CleanupJobOptions
{
    public bool Enabled { get; set; }
    public string Cron { get; set; } = "0 * * * *";
    public int SoftDeleteRetentionDays { get; set; } = 30;
    public int StaleJobRetentionDays { get; set; } = 90;
    public int BatchSize { get; set; } = 100;
}
