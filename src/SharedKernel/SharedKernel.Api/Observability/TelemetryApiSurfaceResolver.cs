using Microsoft.AspNetCore.Http;

namespace SharedKernel.Api.Observability;

/// <summary>
/// Determines the API surface type ("rest" or "graphql") from endpoint metadata.
/// </summary>
public static class TelemetryApiSurfaceResolver
{
    private const string Rest = "rest";
    private const string GraphQL = "graphql";

    public static string Resolve(HttpContext httpContext)
    {
        Endpoint? endpoint = httpContext.GetEndpoint();

        if (endpoint is null)
            return Rest;

        // Check if the endpoint display name or metadata indicates a GraphQL endpoint.
        if (
            endpoint.DisplayName is not null
            && endpoint.DisplayName.Contains("graphql", StringComparison.OrdinalIgnoreCase)
        )
        {
            return GraphQL;
        }

        // Check endpoint metadata for GraphQL marker types.
        foreach (object metadata in endpoint.Metadata)
        {
            string typeName = metadata.GetType().Name;
            if (typeName.Contains("GraphQL", StringComparison.OrdinalIgnoreCase))
                return GraphQL;
        }

        return Rest;
    }
}
