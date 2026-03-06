using System.Threading.RateLimiting;
using APITemplate.Api.ExceptionHandling;
using APITemplate.Api.Filters;
using APITemplate.Api.OpenApi;
using APITemplate.Application.Common.Options;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

namespace APITemplate.Extensions;

public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers core API services (controllers, OpenAPI, ProblemDetails) and
    /// exception handling dependencies.
    /// </summary>
    /// <remarks>
    /// This method only registers exception handling services in DI
    /// (including <see cref="ApiExceptionHandler"/>). Runtime exception interception
    /// is activated later by calling <c>app.UseExceptionHandler()</c> in the middleware pipeline.
    /// </remarks>
    public static IServiceCollection AddApiFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<FluentValidationActionFilter>();
        });
        services.AddProblemDetails(ApiProblemDetailsOptions.Configure);

        // Registers the handler in DI; middleware activation happens in UseApiPipeline via app.UseExceptionHandler().
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeDocumentTransformer>();
            options.AddDocumentTransformer<HealthCheckOpenApiDocumentTransformer>();
            options.AddDocumentTransformer<ProblemDetailsOpenApiTransformer>();
            options.AddOperationTransformer<AuthorizationResponsesOperationTransformer>();
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("fixed", o =>
            {
                o.PermitLimit = 100;
                o.Window = TimeSpan.FromMinutes(1);
            });
        });

        // Output Cache with optional Valkey backing store.
        // When Valkey:ConnectionString is configured, cached responses are stored in Valkey
        // so all application instances share the same cache. Without it, falls back to in-memory.
        // Each policy defines an expiration time and a tag used for targeted invalidation
        // via IOutputCacheStore.EvictByTagAsync() in controllers after mutations (Create/Update/Delete).
        var valkeySection = configuration.GetSection("Valkey");
        var valkeyConnectionString = valkeySection.GetValue<string>("ConnectionString");

        if (!string.IsNullOrEmpty(valkeyConnectionString))
        {
            services.AddOptions<ValkeyOptions>()
                .Bind(valkeySection)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddStackExchangeRedisOutputCache(options =>
            {
                options.Configuration = valkeyConnectionString;
                options.InstanceName = "ApiTemplate:OutputCache:";
            });

            services.AddHealthChecks()
                .AddRedis(valkeyConnectionString, name: "valkey", tags: ["cache"]);
        }
        else
        {
            Log.Warning("Valkey:ConnectionString is not configured — using in-memory output cache. " +
                        "This is not suitable for multi-instance deployments");
        }

        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.NoCache());
            options.AddPolicy("Products", builder => builder.Expire(TimeSpan.FromSeconds(30)).Tag("Products"));
            options.AddPolicy("Categories", builder => builder.Expire(TimeSpan.FromSeconds(60)).Tag("Categories"));
            options.AddPolicy("Reviews", builder => builder.Expire(TimeSpan.FromSeconds(30)).Tag("Reviews"));
        });

        return services;
    }
}
