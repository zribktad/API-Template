using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Authentication-related telemetry facade for shared HTTP authentication flows.
/// </summary>
public static class AuthTelemetry
{
    private static readonly Counter<long> AuthFailures =
        ObservabilityConventions.SharedMeter.CreateCounter<long>(TelemetryMetricNames.AuthFailures);

    public static void RecordMissingTenantClaim(HttpContext httpContext, string scheme) =>
        RecordFailure(
            TelemetryActivityNames.TokenValidated,
            scheme,
            TelemetryFailureReasons.MissingTenantClaim,
            TelemetryApiSurfaceResolver.Resolve(httpContext.Request.Path)
        );

    public static void RecordAuthenticationFailed(
        HttpContext httpContext,
        string scheme,
        Exception exception
    ) =>
        RecordFailure(
            TelemetryActivityNames.TokenValidated,
            scheme,
            TelemetryFailureReasons.AuthenticationFailed,
            TelemetryApiSurfaceResolver.Resolve(httpContext.Request.Path),
            exception
        );

    private static void RecordFailure(
        string activityName,
        string scheme,
        string reason,
        string surface,
        Exception? exception = null
    )
    {
        AuthFailures.Add(
            1,
            new TagList
            {
                { TelemetryTagKeys.AuthScheme, scheme },
                { TelemetryTagKeys.AuthFailureReason, reason },
                { TelemetryTagKeys.ApiSurface, surface },
            }
        );

        using Activity? activity = ObservabilityConventions.SharedActivitySource.StartActivity(
            activityName,
            ActivityKind.Internal
        );
        activity?.SetTag(TelemetryTagKeys.AuthScheme, scheme);
        activity?.SetTag(TelemetryTagKeys.AuthFailureReason, reason);
        activity?.SetTag(TelemetryTagKeys.ApiSurface, surface);
        activity?.SetStatus(ActivityStatusCode.Error);
        if (exception is not null)
            activity?.SetTag(TelemetryTagKeys.ExceptionType, exception.GetType().Name);
    }
}
