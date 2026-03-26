using System.ComponentModel.DataAnnotations;

namespace BackgroundJobs.Application.Options;

/// <summary>
/// Configuration for the TickerQ scheduler, including distributed coordination and fail-safe behaviour.
/// </summary>
public sealed class TickerQSchedulerOptions
{
    public const string DefaultSchemaName = "tickerq";
    public const string DefaultCoordinationConnection = "Dragonfly";

    public bool Enabled { get; init; }
    public bool FailClosed { get; init; } = true;

    [Required]
    public string InstanceNamePrefix { get; init; } = "BackgroundJobs";

    [Required]
    public string CoordinationConnection { get; init; } = DefaultCoordinationConnection;
}
