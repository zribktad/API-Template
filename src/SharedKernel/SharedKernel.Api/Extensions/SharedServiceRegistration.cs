using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Api.Security;
using SharedKernel.Application.Context;
using SharedKernel.Application.Options;
using SharedKernel.Domain.Interfaces;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.SoftDelete;
using SharedKernel.Infrastructure.Persistence.UnitOfWork;
using SharedKernel.Infrastructure.Startup;

namespace SharedKernel.Api.Extensions;

/// <summary>
/// Centralized registration of shared infrastructure services used by tenant-aware microservices
/// that rely on EF Core, UnitOfWork, multi-tenancy, and auditing.
/// </summary>
public static class SharedServiceRegistration
{
    /// <summary>
    /// Registers the shared infrastructure services: UnitOfWork, transaction provider,
    /// auditable entity state manager, soft-delete processor, HTTP context accessor,
    /// tenant/actor providers, TimeProvider, and API versioning.
    /// </summary>
    /// <typeparam name="TDbContext">The concrete EF Core DbContext for the calling service.</typeparam>
    public static IServiceCollection AddSharedInfrastructure<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration
    )
        where TDbContext : DbContext
    {
        // Transaction & Unit of Work
        services.AddValidatedOptions<TransactionDefaultsOptions>(
            configuration,
            TransactionDefaultsOptions.SectionName
        );
        services.AddScoped<IDbTransactionProvider, EfCoreTransactionProvider>();
        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(
            sp.GetRequiredService<TDbContext>(),
            sp.GetRequiredService<IOptions<TransactionDefaultsOptions>>(),
            sp.GetRequiredService<ILogger<UnitOfWork>>(),
            sp.GetRequiredService<IDbTransactionProvider>()
        ));

        // Auditing & Soft Delete
        services.AddScoped<IAuditableEntityStateManager, AuditableEntityStateManager>();
        services.AddScoped<ISoftDeleteProcessor, SoftDeleteProcessor>();

        // Context providers
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, HttpTenantProvider>();
        services.AddScoped<IActorProvider, HttpActorProvider>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<
            IStartupTaskCoordinator,
            PostgresAdvisoryLockStartupTaskCoordinator
        >();

        // API versioning
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
