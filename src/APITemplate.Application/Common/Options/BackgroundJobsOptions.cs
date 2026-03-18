namespace APITemplate.Application.Common.Options;

public sealed class BackgroundJobsOptions
{
    public TickerQSchedulerOptions TickerQ { get; set; } = new();
    public ExternalSyncJobOptions ExternalSync { get; set; } = new();
    public CleanupJobOptions Cleanup { get; set; } = new();
    public ReindexJobOptions Reindex { get; set; } = new();
    public EmailRetryJobOptions EmailRetry { get; set; } = new();
}
