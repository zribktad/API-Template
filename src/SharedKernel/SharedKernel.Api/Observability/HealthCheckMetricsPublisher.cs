using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SharedKernel.Api.Observability;

/// <summary>
/// Publishes health-check results as observable gauge metrics so they can be scraped by
/// OpenTelemetry collectors (1 = Healthy, 0.5 = Degraded, 0 = Unhealthy).
/// </summary>
public sealed class HealthCheckMetricsPublisher : IHealthCheckPublisher
{
    private static readonly Meter Meter = new(ObservabilityConventions.MeterName);

    private readonly object _lock = new();
    private readonly Dictionary<string, double> _results = new();

    public HealthCheckMetricsPublisher()
    {
        Meter.CreateObservableGauge(
            "api.health_check.status",
            observeValues: ObserveValues,
            description: "Health check status (1=Healthy, 0.5=Degraded, 0=Unhealthy)"
        );
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            foreach (KeyValuePair<string, HealthReportEntry> entry in report.Entries)
            {
                _results[entry.Key] = entry.Value.Status switch
                {
                    HealthStatus.Healthy => 1.0,
                    HealthStatus.Degraded => 0.5,
                    HealthStatus.Unhealthy => 0.0,
                    _ => 0.0,
                };
            }
        }

        return Task.CompletedTask;
    }

    private IEnumerable<Measurement<double>> ObserveValues()
    {
        lock (_lock)
        {
            foreach (KeyValuePair<string, double> entry in _results)
            {
                yield return new Measurement<double>(
                    entry.Value,
                    new KeyValuePair<string, object?>("check_name", entry.Key)
                );
            }
        }
    }
}
