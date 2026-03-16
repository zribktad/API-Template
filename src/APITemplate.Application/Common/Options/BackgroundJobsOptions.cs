namespace APITemplate.Application.Common.Options;

public sealed class BackgroundJobsOptions
{
    public CleanupJobOptions Cleanup { get; set; } = new();
    public ReindexJobOptions Reindex { get; set; } = new();
    public EmailRetryJobOptions EmailRetry { get; set; } = new();
}
