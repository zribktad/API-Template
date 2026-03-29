using System.Diagnostics.Metrics;

namespace SharedKernel.Api.Observability.Telemetry;

/// <summary>
/// Counters for cache hit/miss tracking.
/// </summary>
public static class CacheTelemetry
{
    private static readonly Meter Meter = new(ObservabilityConventions.MeterName);

    private static readonly Counter<long> CacheHits = Meter.CreateCounter<long>(
        "api.cache.hits",
        description: "Number of cache hits"
    );

    private static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>(
        "api.cache.misses",
        description: "Number of cache misses"
    );

    public static void RecordCacheHit(string cacheTag) =>
        CacheHits.Add(1, new KeyValuePair<string, object?>("cache_tag", cacheTag));

    public static void RecordCacheMiss(string cacheTag) =>
        CacheMisses.Add(1, new KeyValuePair<string, object?>("cache_tag", cacheTag));
}
