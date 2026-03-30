using FluentValidation.Results;

namespace SharedKernel.Application.Batch.Rules;

/// <summary>
/// Abstraction for recording validation failure metrics. Implemented in the infrastructure
/// layer so the application layer stays free of telemetry dependencies.
/// </summary>
public interface IValidationMetrics
{
    void RecordFailure(string source, Type argumentType, IReadOnlyList<ValidationFailure> failures);
}
