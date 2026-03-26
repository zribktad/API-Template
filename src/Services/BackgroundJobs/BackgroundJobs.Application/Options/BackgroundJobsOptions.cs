namespace BackgroundJobs.Application.Options;

/// <summary>
/// Aggregates per-job configuration options for all registered background jobs in the service.
/// </summary>
public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    public TickerQSchedulerOptions TickerQ { get; set; } = new();
    public CleanupJobOptions Cleanup { get; set; } = new();
    public ReindexJobOptions Reindex { get; set; } = new();
}
