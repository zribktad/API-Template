using Microsoft.AspNetCore.Http;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Maps an HTTP request path to a logical API surface name for use as a telemetry tag value.
/// </summary>
public static class TelemetryApiSurfaceResolver
{
    public static string Resolve(PathString path)
    {
        if (path.StartsWithSegments(TelemetryPathPrefixes.GraphQl))
            return TelemetrySurfaces.GraphQl;

        if (path.StartsWithSegments(TelemetryPathPrefixes.Health))
            return TelemetrySurfaces.Health;

        if (
            path.StartsWithSegments(TelemetryPathPrefixes.Scalar)
            || path.StartsWithSegments(TelemetryPathPrefixes.OpenApi)
        )
        {
            return TelemetrySurfaces.Documentation;
        }

        return TelemetrySurfaces.Rest;
    }
}
