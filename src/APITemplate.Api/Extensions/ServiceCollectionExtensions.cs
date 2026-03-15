using APITemplate.Api.Cache;
using APITemplate.Application.Common.Behaviors;
using APITemplate.Application.Common.Context;
using APITemplate.Application.Features.Product;
using APITemplate.Application.Features.Product.Validation;
using APITemplate.Infrastructure.Security;
using Asp.Versioning;
using FluentValidation;
using MediatR;

namespace APITemplate.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application-layer services for dependency injection.
    /// </summary>
    /// <remarks>
    /// This includes:
    /// - access to HTTP context (for tenant/actor resolution),
    /// - application services that provide current tenant/actor identity,
    /// - FluentValidation validators,
    /// - and MediatR handlers + pipeline behaviors.
    /// </remarks>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Provides access to HttpContext in places that are not controllers/middleware.
        services.AddHttpContextAccessor();

        // Tenant/actor context providers used in application logic (e.g. auditing, authorization).
        services.AddScoped<ITenantProvider, HttpTenantProvider>();
        services.AddScoped<IActorProvider, HttpActorProvider>();

        // Register FluentValidation validators from the application layer.
        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

        // Register MediatR request/notification handlers and behaviors.
        services.AddMediatR(cfg =>
        {
            // Scan the application layer for MediatR handlers.
            cfg.RegisterServicesFromAssemblyContaining<CreateProductCommand>();
            cfg.RegisterServicesFromAssemblyContaining<CacheInvalidationNotificationHandler>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }

    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        // Enable API versioning and configure how clients specify the version.
        // This is used both for routing and for generating correct OpenAPI/Swagger docs.
        services
            .AddApiVersioning(options =>
            {
                // Default to v1 when the client does not specify a version.
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;

                // Include supported/deprecated version headers in responses.
                options.ReportApiVersions = true;

                // Read the version from the URL segment (e.g. /api/v1/...).
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                // Format how OpenAPI groups endpoints by version (v1, v2, ...).
                options.GroupNameFormat = "'v'VVV";
                // Replace the {version} placeholder in route templates with the actual version.
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
