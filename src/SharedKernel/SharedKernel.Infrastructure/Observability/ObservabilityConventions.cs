namespace SharedKernel.Infrastructure.Observability;

/// <summary>Canonical tag/attribute key names applied to metrics and traces.</summary>
public static class TelemetryTagKeys
{
    public const string ApiSurface = "apitemplate.api.surface";
    public const string Authenticated = "apitemplate.authenticated";
    public const string TenantId = "tenant.id";
}

/// <summary>Well-known tag values that identify the API surface a request was served from.</summary>
public static class TelemetrySurfaces
{
    public const string Documentation = "documentation";
    public const string GraphQl = "graphql";
    public const string Health = "health";
    public const string Rest = "rest";
}

/// <summary>URL path prefixes used to classify requests into API surface areas.</summary>
public static class TelemetryPathPrefixes
{
    public const string GraphQl = "/graphql";
    public const string Health = "/health";
    public const string OpenApi = "/openapi";
    public const string Scalar = "/scalar";
}
