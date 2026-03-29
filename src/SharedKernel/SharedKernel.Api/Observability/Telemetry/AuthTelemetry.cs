using System.Diagnostics;

namespace SharedKernel.Api.Observability.Telemetry;

/// <summary>
/// Distributed-tracing activities for authentication flows.
/// </summary>
public static class AuthTelemetry
{
    private static readonly ActivitySource Source = new(
        ObservabilityConventions.ActivitySourceName
    );

    public static Activity? StartLoginActivity(string username)
    {
        Activity? activity = Source.StartActivity("auth.login");
        activity?.SetTag("auth.username", username);
        return activity;
    }

    public static Activity? StartTokenRefreshActivity()
    {
        return Source.StartActivity("auth.token_refresh");
    }
}
