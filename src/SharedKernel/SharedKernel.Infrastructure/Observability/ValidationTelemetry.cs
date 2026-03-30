using System.Diagnostics.Metrics;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.Filters;
using SharedKernel.Application.Batch.Rules;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Validation-related metrics facade. Implements <see cref="IValidationMetrics"/> for DI use
/// and exposes a static HTTP overload for use in MVC action filters.
/// </summary>
public sealed class ValidationTelemetry : IValidationMetrics
{
    private static readonly Meter Meter = new(ObservabilityConventions.MeterName);

    private static readonly Counter<long> ValidationRequestsRejected = Meter.CreateCounter<long>(
        TelemetryMetricNames.ValidationRequestsRejected
    );

    private static readonly Counter<long> ValidationErrors = Meter.CreateCounter<long>(
        TelemetryMetricNames.ValidationErrors
    );

    /// <inheritdoc/>
    public void RecordFailure(
        string source,
        Type argumentType,
        IReadOnlyList<ValidationFailure> failures
    )
    {
        ValidationRequestsRejected.Add(
            1,
            [
                new KeyValuePair<string, object?>(
                    TelemetryTagKeys.ValidationDtoType,
                    argumentType.Name
                ),
                new KeyValuePair<string, object?>(TelemetryTagKeys.HttpRoute, source),
            ]
        );

        foreach (ValidationFailure failure in failures)
        {
            ValidationErrors.Add(
                1,
                [
                    new KeyValuePair<string, object?>(
                        TelemetryTagKeys.ValidationDtoType,
                        argumentType.Name
                    ),
                    new KeyValuePair<string, object?>(TelemetryTagKeys.HttpRoute, source),
                    new KeyValuePair<string, object?>(
                        TelemetryTagKeys.ValidationProperty,
                        failure.PropertyName
                    ),
                ]
            );
        }
    }

    /// <summary>
    /// Records a validation failure from an MVC action filter, resolving the route from
    /// <paramref name="context"/>.
    /// </summary>
    public static void RecordFromActionFilter(
        ActionExecutingContext context,
        Type argumentType,
        IEnumerable<ValidationFailure> failures
    )
    {
        string route = context.ActionDescriptor.AttributeRouteInfo?.Template is { } template
            ? HttpRouteResolver.ReplaceVersionToken(template, context.RouteData.Values)
            : context.HttpContext.Request.Path.Value ?? TelemetryDefaults.Unknown;

        Instance.RecordFailure(route, argumentType, failures.ToList());
    }

    /// <summary>Singleton used by the static <see cref="RecordFromActionFilter"/> helper.</summary>
    internal static readonly ValidationTelemetry Instance = new();
}
