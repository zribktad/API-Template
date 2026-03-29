using System.ComponentModel.DataAnnotations;

namespace BackgroundJobs.Application.Options;

/// <summary>
/// Configuration for the periodic email retry job that re-attempts delivery of failed emails
/// stored in the Notifications database, with claim-based concurrency and dead-lettering.
/// </summary>
public sealed class EmailRetryJobOptions
{
    public bool Enabled { get; init; } = true;

    [Required]
    public string Cron { get; init; } = "*/15 * * * *";

    [Range(1, 20)]
    public int MaxRetryAttempts { get; init; } = 5;

    [Range(1, 500)]
    public int BatchSize { get; init; } = 50;

    [Range(1, int.MaxValue)]
    public int DeadLetterAfterHours { get; init; } = 48;

    [Range(1, int.MaxValue)]
    public int ClaimDurationMinutes { get; init; } = 10;
}
