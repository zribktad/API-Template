using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace SharedKernel.Infrastructure.Logging;

/// <summary>
/// Serilog <see cref="ILogEventEnricher"/> that appends W3C-format <c>TraceId</c> and <c>SpanId</c>
/// properties from the current <see cref="Activity"/> to every log event,
/// enabling correlation between structured logs and distributed traces.
/// </summary>
public sealed class ActivityTraceEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        Activity? activity = Activity.Current;
        if (activity is null)
            return;

        if (activity.TraceId != default)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("TraceId", activity.TraceId.ToHexString())
            );
        }

        if (activity.SpanId != default)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("SpanId", activity.SpanId.ToHexString())
            );
        }
    }
}
