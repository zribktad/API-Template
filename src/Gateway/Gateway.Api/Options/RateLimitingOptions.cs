using System.ComponentModel.DataAnnotations;

namespace Gateway.Api.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting:Fixed";

    [Range(1, 10000)]
    public int PermitLimit { get; init; } = 100;

    [Range(1, 60)]
    public int WindowMinutes { get; init; } = 1;
}
