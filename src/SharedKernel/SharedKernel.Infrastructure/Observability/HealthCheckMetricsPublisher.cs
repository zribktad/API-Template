using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Publishes health check results as observable gauge metrics.
/// </summary>
public sealed class HealthCheckMetricsPublisher : IHealthCheckPublisher
{
    private static readonly Meter Meter = new(ObservabilityConventions.HealthMeterName);
    private static readonly ConcurrentDictionary<string, int> Statuses = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly ObservableGauge<int> _gauge;

    public HealthCheckMetricsPublisher()
    {
        _gauge = Meter.CreateObservableGauge(TelemetryMetricNames.HealthStatus, ObserveStatuses);
    }

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
