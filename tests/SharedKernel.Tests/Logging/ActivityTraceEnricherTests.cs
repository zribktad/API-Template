using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;
using SharedKernel.Infrastructure.Logging;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Logging;

public sealed class ActivityTraceEnricherTests
{
    private readonly ActivityTraceEnricher _sut = new();

    [Fact]
    public void Enrich_WithActiveActivity_AddsTraceIdAndSpanId()
    {
        using ActivityListener listener = new()
        {
            ShouldListenTo = static _ => true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using ActivitySource source = new("SharedKernel.Tests");
        using Activity? activity = source.StartActivity("test-operation");

        activity.ShouldNotBeNull();

        LogEvent logEvent = CreateLogEvent();
        _sut.Enrich(logEvent, new TestLogEventPropertyFactory());

        logEvent.Properties.ContainsKey("TraceId").ShouldBeTrue();
        logEvent.Properties.ContainsKey("SpanId").ShouldBeTrue();
        ((ScalarValue)logEvent.Properties["TraceId"]).Value.ShouldBe(
            activity.TraceId.ToHexString()
        );
        ((ScalarValue)logEvent.Properties["SpanId"]).Value.ShouldBe(activity.SpanId.ToHexString());
    }

    [Fact]
    public void Enrich_WithNoActivity_AddsNoProperties()
    {
        Activity.Current = null;

        LogEvent logEvent = CreateLogEvent();
        _sut.Enrich(logEvent, new TestLogEventPropertyFactory());

        logEvent.Properties.ContainsKey("TraceId").ShouldBeFalse();
        logEvent.Properties.ContainsKey("SpanId").ShouldBeFalse();
    }

    private static LogEvent CreateLogEvent() =>
        new(DateTimeOffset.UtcNow, LogEventLevel.Information, null, MessageTemplate.Empty, []);

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(
            string name,
            object? value,
            bool destructureObjects = false
        ) => new(name, new ScalarValue(value));
    }
}
