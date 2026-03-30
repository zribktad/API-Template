using System.Diagnostics.Metrics;
using SharedKernel.Domain.Exceptions;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Conflict-related metrics facade.
/// </summary>
public static class ConflictTelemetry
{
    private static readonly Meter Meter = new(ObservabilityConventions.MeterName);

    private static readonly Counter<long> ConcurrencyConflicts = Meter.CreateCounter<long>(
        TelemetryMetricNames.ConcurrencyConflicts
    );

    private static readonly Counter<long> DomainConflicts = Meter.CreateCounter<long>(
        TelemetryMetricNames.DomainConflicts
    );

    public static void Record(Exception exception, string errorCode)
    {
        if (exception is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            ConcurrencyConflicts.Add(1);
            return;
        }

        if (exception is ConflictException)
        {
            DomainConflicts.Add(
                1,
                [new KeyValuePair<string, object?>(TelemetryTagKeys.ErrorCode, errorCode)]
            );
        }
    }
}
