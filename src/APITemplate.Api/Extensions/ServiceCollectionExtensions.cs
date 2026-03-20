using APITemplate.Api.Cache;
using APITemplate.Api.Events;
using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.CQRS.Decorators;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product;
using APITemplate.Application.Features.Product.Validation;
using APITemplate.Infrastructure.Security;
using Asp.Versioning;
using FluentValidation;

namespace APITemplate.Api.Extensions;

/// <summary>
/// Presentation-layer extension class that registers application services (CQRS handlers, validators,
/// tenant/actor context providers) and API versioning configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application-layer services for dependency injection.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ITenantProvider, HttpTenantProvider>();
        services.AddScoped<IActorProvider, HttpActorProvider>();

        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

        services.AddCqrsHandlers();

        return services;
    }

    /// <summary>
    /// Registers all CQRS command/query/event handlers via Scrutor assembly scanning,
    /// wraps command handlers with validation decorators, and registers the event publisher.
    /// </summary>
    private static void AddCqrsHandlers(this IServiceCollection services)
    {
        var applicationAssembly = typeof(CreateProductCommand).Assembly;
        var apiAssembly = typeof(CacheInvalidationHandler<>).Assembly;

        services.Scan(scan =>
            scan.FromAssemblies(applicationAssembly, apiAssembly)
                .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        // Closed generic — open generic registration would break non-cache events at runtime
        services.AddScoped<
            IDomainEventHandler<CacheInvalidationNotification>,
            CacheInvalidationHandler<CacheInvalidationNotification>
        >();

        // Validation decorators for command handlers
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(ValidationCommandHandlerDecorator<>));

        // Event publisher
        services.AddScoped<IEventPublisher, EventPublisher>();
    }

    /// <summary>
    /// Configures URL-segment API versioning (defaulting to v1) and the API explorer used by
    /// OpenAPI to group endpoints by version.
    /// </summary>
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
