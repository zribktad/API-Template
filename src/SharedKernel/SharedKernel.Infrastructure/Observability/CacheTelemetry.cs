using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Output-cache telemetry facade for invalidation activities and cache outcome metrics.
/// </summary>
public static class CacheTelemetry
{
    private static readonly Counter<long> OutputCacheInvalidations =
        ObservabilityConventions.SharedMeter.CreateCounter<long>(
            TelemetryMetricNames.OutputCacheInvalidations
        );

    private static readonly Histogram<double> OutputCacheInvalidationDurationMs =
        ObservabilityConventions.SharedMeter.CreateHistogram<double>(
            TelemetryMetricNames.OutputCacheInvalidationDuration,
            unit: "ms"
        );

    private static readonly Counter<long> OutputCacheOutcomes =
        ObservabilityConventions.SharedMeter.CreateCounter<long>(
            TelemetryMetricNames.OutputCacheOutcomes
        );

    public static Activity? StartOutputCacheInvalidationActivity(string tag)
    {
        Activity? activity = ObservabilityConventions.SharedActivitySource.StartActivity(
            TelemetryActivityNames.OutputCacheInvalidate,
            ActivityKind.Internal
        );
        activity?.SetTag(TelemetryTagKeys.CacheTag, tag);
        return activity;
    }

    public static void RecordOutputCacheInvalidation(string tag, TimeSpan duration)
    {
        TagList tags = new() { { TelemetryTagKeys.CacheTag, tag } };
        OutputCacheInvalidations.Add(1, tags);
        OutputCacheInvalidationDurationMs.Record(duration.TotalMilliseconds, tags);
    }

    public static void ConfigureRequest(OutputCacheContext context)
    {
        context.HttpContext.Items[TelemetryContextKeys.OutputCachePolicyName] = ResolvePolicyName(
            context
        );
    }

    public static void RecordCacheHit(OutputCacheContext context) =>
        RecordCacheOutcome(context, TelemetryOutcomeValues.Hit);

    public static void RecordResponseOutcome(OutputCacheContext context)
    {
        string outcome = context.AllowCacheStorage
            ? TelemetryOutcomeValues.Store
            : TelemetryOutcomeValues.Bypass;
        RecordCacheOutcome(context, outcome);
    }

    private static void RecordCacheOutcome(OutputCacheContext context, string outcome)
    {
        TagList tags = new()
        {
            { TelemetryTagKeys.CachePolicy, ResolvePolicyName(context) },
            {
                TelemetryTagKeys.ApiSurface,
                TelemetryApiSurfaceResolver.Resolve(context.HttpContext.Request.Path)
            },
            { TelemetryTagKeys.CacheOutcome, outcome },
        };
        OutputCacheOutcomes.Add(1, tags);
    }

    private static string ResolvePolicyName(OutputCacheContext context)
    {
        if (
            context.HttpContext.Items.TryGetValue(
                TelemetryContextKeys.OutputCachePolicyName,
                out object? cached
            ) && cached is string name
        )
        {
            return name;
        }

        return context
                .HttpContext.GetEndpoint()
                ?.Metadata.OfType<OutputCacheAttribute>()
                .Select(attribute => attribute.PolicyName)
                .FirstOrDefault(policyName => !string.IsNullOrWhiteSpace(policyName))
            ?? TelemetryDefaults.Default;
    }
}
