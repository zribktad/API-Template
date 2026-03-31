using System.ComponentModel.DataAnnotations;

namespace BackgroundJobs.Application.Options;

/// <summary>
/// Configuration for the scheduled job that rebuilds search indexes on a periodic basis.
/// </summary>
public sealed class ReindexJobOptions
{
    public bool Enabled { get; init; }

    [Required]
    public string Cron { get; init; } = "0 */6 * * *";
}
