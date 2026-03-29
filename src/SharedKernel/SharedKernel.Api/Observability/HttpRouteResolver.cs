using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SharedKernel.Api.Observability;

/// <summary>
/// Resolves the route template from an <see cref="HttpContext"/> to produce
/// low-cardinality labels suitable for metrics (avoids high-cardinality path parameters).
/// </summary>
public static class HttpRouteResolver
{
    private const string UnknownRoute = "unknown";

    public static string GetRouteTemplate(HttpContext httpContext)
    {
        Endpoint? endpoint = httpContext.GetEndpoint();

        if (endpoint is RouteEndpoint routeEndpoint)
            return routeEndpoint.RoutePattern.RawText ?? UnknownRoute;

        return UnknownRoute;
    }
}
