using System.Diagnostics;
using System.Diagnostics.Metrics;
using SharedKernel.Domain.Exceptions;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Conflict-related metrics facade.
/// </summary>
public static class ConflictTelemetry
{
    private static readonly Counter<long> ConcurrencyConflicts =
        ObservabilityConventions.SharedMeter.CreateCounter<long>(
            TelemetryMetricNames.ConcurrencyConflicts
        );

    private static readonly Counter<long> DomainConflicts =
        ObservabilityConventions.SharedMeter.CreateCounter<long>(
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
            DomainConflicts.Add(1, new TagList { { TelemetryTagKeys.ErrorCode, errorCode } });
        }
    }
}
