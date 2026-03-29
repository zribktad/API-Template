using System.Diagnostics.Metrics;

namespace SharedKernel.Api.Observability.Telemetry;

/// <summary>
/// Counter for validation failure tracking.
/// </summary>
public static class ValidationTelemetry
{
    private static readonly Meter Meter = new(ObservabilityConventions.MeterName);

    private static readonly Counter<long> ValidationFailures = Meter.CreateCounter<long>(
        "api.validation.failures",
        description: "Number of validation failures"
    );

    public static void RecordValidationFailure(string errorCode) =>
        ValidationFailures.Add(1, new KeyValuePair<string, object?>("error_code", errorCode));
}
