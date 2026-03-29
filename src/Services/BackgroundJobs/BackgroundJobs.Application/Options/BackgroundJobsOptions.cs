using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace BackgroundJobs.Application.Options;

/// <summary>
/// Aggregates per-job configuration options for all registered background jobs in the service.
/// </summary>
public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    [Required]
    [ValidateObjectMembers]
    public TickerQSchedulerOptions TickerQ { get; init; } = new();

    [Required]
    [ValidateObjectMembers]
    public CleanupJobOptions Cleanup { get; init; } = new();

    [Required]
    [ValidateObjectMembers]
    public ReindexJobOptions Reindex { get; init; } = new();

    [Required]
    [ValidateObjectMembers]
    public EmailRetryJobOptions EmailRetry { get; init; } = new();
}
