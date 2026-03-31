using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Publishes health check results as observable gauge metrics.
/// </summary>
public sealed class HealthCheckMetricsPublisher : IHealthCheckPublisher
{
    private static readonly ConcurrentDictionary<string, int> Statuses = new(
        StringComparer.OrdinalIgnoreCase
    );

    // Static gauge — registering multiple instances on the same Meter causes duplicate metrics.
    private static readonly ObservableGauge<int> Gauge =
        ObservabilityConventions.SharedHealthMeter.CreateObservableGauge(
            TelemetryMetricNames.HealthStatus,
            ObserveStatuses
        );

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach ((string key, HealthReportEntry value) in report.Entries)
        {
            Statuses[key] = value.Status == HealthStatus.Healthy ? 1 : 0;
        }

        return Task.CompletedTask;
    }

    internal static IReadOnlyDictionary<string, int> SnapshotStatuses() => Statuses;

    private static IEnumerable<Measurement<int>> ObserveStatuses()
    {
        foreach ((string key, int value) in Statuses)
        {
            yield return new Measurement<int>(
                value,
                new KeyValuePair<string, object?>(TelemetryTagKeys.Service, key)
            );
        }
    }
}
