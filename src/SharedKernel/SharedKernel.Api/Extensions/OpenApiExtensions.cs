using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Api.OpenApi;
using SharedKernel.Application.Security;

namespace SharedKernel.Api.Extensions;

public static class OpenApiExtensions
{
    /// <summary>
    /// Registers the shared OpenAPI pipeline (OAuth2 scheme + standardized error responses).
    /// </summary>
    public static IServiceCollection AddSharedOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(
            SharedAuthConstants.OpenApi.DefaultDocumentName,
            options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeDocumentTransformer>();
                options.AddDocumentTransformer<ProblemDetailsOpenApiTransformer>();
                options.AddOperationTransformer<AuthorizationResponsesOperationTransformer>();
            }
        );

        return services;
    }

    /// <summary>
    /// Maps the default OpenAPI JSON endpoint in development.
    /// </summary>
    public static WebApplication MapSharedOpenApiEndpoint(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapOpenApi().AllowAnonymous();
        return app;
    }
}
