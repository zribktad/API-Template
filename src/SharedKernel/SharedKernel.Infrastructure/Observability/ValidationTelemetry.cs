using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentValidation.Results;
using SharedKernel.Application.Batch.Rules;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Validation-related metrics facade implementing <see cref="IValidationMetrics"/> for DI use.
/// </summary>
public sealed class ValidationTelemetry : IValidationMetrics
{
    private static readonly Counter<long> ValidationRequestsRejected =
        ObservabilityConventions.SharedMeter.CreateCounter<long>(
            TelemetryMetricNames.ValidationRequestsRejected
        );

    private static readonly Counter<long> ValidationErrors =
        ObservabilityConventions.SharedMeter.CreateCounter<long>(
            TelemetryMetricNames.ValidationErrors
        );

    /// <inheritdoc/>
    public void RecordFailure(
        string source,
        Type argumentType,
        IReadOnlyList<ValidationFailure> failures
    )
    {
        TagList requestTags = new()
        {
            { TelemetryTagKeys.ValidationDtoType, argumentType.Name },
            { TelemetryTagKeys.HttpRoute, source },
        };
        ValidationRequestsRejected.Add(1, requestTags);

        foreach (ValidationFailure failure in failures)
        {
            TagList errorTags = new()
            {
                { TelemetryTagKeys.ValidationDtoType, argumentType.Name },
                { TelemetryTagKeys.HttpRoute, source },
                { TelemetryTagKeys.ValidationProperty, failure.PropertyName },
            };
            ValidationErrors.Add(1, errorTags);
        }
    }
}
