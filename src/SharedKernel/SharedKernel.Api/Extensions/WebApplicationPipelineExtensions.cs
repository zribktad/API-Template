using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Api.Extensions;

/// <summary>
/// Shared HTTP pipeline helpers for microservice API hosts.
/// </summary>
public static class WebApplicationPipelineExtensions
{
    /// <summary>
    /// Registers the outermost exception handler and authentication middleware.
    /// Call this first, then add any service-specific middleware (e.g. CSRF),
    /// then call <see cref="UseSharedAuthorizationCachingAndInfrastructure"/>.
    /// </summary>
    public static WebApplication UseSharedExceptionHandlerAndAuthentication(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseAuthentication();
        return app;
    }

    /// <summary>
    /// Registers authorization, optional output cache, OpenAPI endpoint, and health checks.
    /// Call this after <see cref="UseSharedExceptionHandlerAndAuthentication"/> and any service-specific middleware.
    /// Map your endpoints (MapControllers, MapWolverineEndpoints, …) after this call.
    /// </summary>
    public static WebApplication UseSharedAuthorizationCachingAndInfrastructure(
        this WebApplication app,
        bool useOutputCaching
    )
    {
        app.UseAuthorization();
        app.UseRequestContextPipeline();

        if (useOutputCaching)
            app.UseSharedOutputCaching();

        app.MapSharedOpenApiEndpoint();
        app.MapHealthChecks("/health").AllowAnonymous();

        return app;
    }
}
