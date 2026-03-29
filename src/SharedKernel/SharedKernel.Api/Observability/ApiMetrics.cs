using System.Diagnostics.Metrics;

namespace SharedKernel.Api.Observability;

/// <summary>
/// Application-level counters for error tracking and rate-limit monitoring.
/// </summary>
public static class ApiMetrics
{
    private static readonly Meter Meter = new(ObservabilityConventions.MeterName);

    private static readonly Counter<long> RateLimitRejections = Meter.CreateCounter<long>(
        "api.rate_limit.rejections",
        description: "Number of requests rejected by rate limiting"
    );

    private static readonly Counter<long> HandledExceptions = Meter.CreateCounter<long>(
        "api.exceptions.handled",
        description: "Number of handled (expected) exceptions"
    );

    private static readonly Counter<long> UnhandledExceptions = Meter.CreateCounter<long>(
        "api.exceptions.unhandled",
        description: "Number of unhandled (unexpected) exceptions"
    );

    public static void RecordRateLimitRejection(string partitionKey) =>
        RateLimitRejections.Add(
            1,
            new KeyValuePair<string, object?>("partition_key", partitionKey)
        );

    public static void RecordHandledException(string errorCode) =>
        HandledExceptions.Add(1, new KeyValuePair<string, object?>("error_code", errorCode));

    public static void RecordUnhandledException() => UnhandledExceptions.Add(1);
}
