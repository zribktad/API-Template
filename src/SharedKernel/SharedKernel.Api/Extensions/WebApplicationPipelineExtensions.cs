using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Api.Extensions;

/// <summary>
/// Shared HTTP pipeline tail for microservice API hosts (auth, optional output cache, OpenAPI, health, endpoints).
/// </summary>
public static class WebApplicationPipelineExtensions
{
    /// <summary>
    /// Applies the standard middleware order, then maps health checks and custom endpoints.
    /// </summary>
    /// <param name="useOutputCaching">When true, registers <c>UseSharedOutputCaching</c> after authorization.</param>
    /// <param name="mapEndpoints">Map controllers, Wolverine HTTP endpoints, or both.</param>
    /// <param name="configureAfterAuthentication">Optional hook invoked after authentication but before authorization.</param>
    public static WebApplication UseSharedMicroserviceApiPipeline(
        this WebApplication app,
        bool useOutputCaching,
        Action<WebApplication> mapEndpoints,
        Action<WebApplication>? configureAfterAuthentication = null
    )
    {
        ArgumentNullException.ThrowIfNull(mapEndpoints);

        app.UseSharedRequestLogging();
        app.UseAuthentication();
        configureAfterAuthentication?.Invoke(app);
        app.UseAuthorization();

        if (useOutputCaching)
            app.UseSharedOutputCaching();

        app.MapSharedOpenApiEndpoint();
        app.MapHealthChecks("/health").AllowAnonymous();
        mapEndpoints(app);

        return app;
    }
}
