using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Options;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.BackgroundJobs.Services;
using APITemplate.Infrastructure.BackgroundJobs.TickerQ;
using APITemplate.Infrastructure.BackgroundJobs.TickerQ.Coordination;
using APITemplate.Infrastructure.BackgroundJobs.TickerQ.Jobs;
using APITemplate.Infrastructure.BackgroundJobs.TickerQ.RecurringJobRegistrations;
using APITemplate.Infrastructure.BackgroundJobs.Validation;
using APITemplate.Infrastructure.Email;
using APITemplate.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;

namespace APITemplate.Extensions;

public static class BackgroundJobsServiceCollectionExtensions
{
    private static readonly Type[] SoftDeleteCleanupOrder =
    [
        typeof(ProductDataLink),
        typeof(ProductReview),
        typeof(Product),
        typeof(AppUser),
        typeof(TenantInvitation),
        typeof(Category),
        typeof(Tenant),
    ];

    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var section = configuration.SectionFor<BackgroundJobsOptions>();
        services.AddSingleton<
            IValidateOptions<BackgroundJobsOptions>,
            BackgroundJobsOptionsValidator
        >();
        services.AddOptions<BackgroundJobsOptions>().Bind(section).ValidateOnStart();
        var options = section.Get<BackgroundJobsOptions>() ?? new BackgroundJobsOptions();

        RegisterTickerQInfrastructure(services, configuration, options);

        services.AddScoped<IFailedEmailRepository, FailedEmailRepository>();
        services.AddSingleton<IFailedEmailStore, FailedEmailStore>();

        services.AddScoped<ICleanupService, CleanupService>();
        services.AddScoped<IReindexService, ReindexService>();
        services.AddScoped<IEmailRetryService, EmailRetryService>();
        services.AddScoped<
            IExternalIntegrationSyncService,
            ExternalIntegrationSyncServicePreview
        >();

        services.AddScoped<
            IRecurringBackgroundJobRegistration,
            ExternalSyncRecurringJobRegistration
        >();
        services.AddScoped<IRecurringBackgroundJobRegistration, CleanupRecurringJobRegistration>();
        services.AddScoped<IRecurringBackgroundJobRegistration, ReindexRecurringJobRegistration>();
        services.AddScoped<
            IRecurringBackgroundJobRegistration,
            EmailRetryRecurringJobRegistration
        >();

        RegisterSoftDeleteCleanupStrategies(services);

        return services;
    }

    private static void RegisterTickerQInfrastructure(
        IServiceCollection services,
        IConfiguration configuration,
        BackgroundJobsOptions options
    )
    {
        if (!options.TickerQ.Enabled)
        {
            return;
        }

        var dragonflyConnectionString = configuration
            .SectionFor<DragonflyOptions>()
            .GetValue<string>(nameof(DragonflyOptions.ConnectionString));

        if (string.IsNullOrWhiteSpace(dragonflyConnectionString))
        {
            throw new InvalidOperationException(
                "Background jobs require Dragonfly:ConnectionString when BackgroundJobs:TickerQ:Enabled is true."
            );
        }

        if (
            !string.Equals(
                options.TickerQ.CoordinationConnection,
                TickerQSchedulerOptions.DefaultCoordinationConnection,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new InvalidOperationException(
                $"Only '{TickerQSchedulerOptions.DefaultCoordinationConnection}' is supported for BackgroundJobs:TickerQ:CoordinationConnection."
            );
        }

        var connectionString = configuration.GetConnectionString(
            ConfigurationSections.DefaultConnection
        )!;
        var schemaName = TickerQSchedulerOptions.DefaultSchemaName;

        services.AddDbContext<TickerQSchedulerDbContext>(dbOptions =>
            dbOptions.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schemaName)
            )
        );
        services.AddScoped<TickerQRecurringJobRegistrar>();
        services.AddSingleton<IDistributedJobCoordinator, DragonflyDistributedJobCoordinator>();

        services.AddTickerQ(tickerOptions =>
        {
            tickerOptions
                .AddOperationalStore(store =>
                    store
                        .UseApplicationDbContext<TickerQSchedulerDbContext>(
                            ConfigurationType.IgnoreModelCustomizer
                        )
                        .SetSchema(schemaName)
                )
                .ConfigureScheduler(scheduler =>
                {
                    scheduler.NodeIdentifier =
                        $"{options.TickerQ.InstanceNamePrefix}-{Environment.MachineName}-{Environment.ProcessId}";
                    scheduler.MaxConcurrency = 1;
                })
                .AddTickerQDiscovery<TimeTickerEntity, CronTickerEntity>([
                    typeof(CleanupRecurringJob).Assembly,
                ]);
        });
    }

    private static void RegisterSoftDeleteCleanupStrategies(IServiceCollection services)
    {
        var softDeletableTypes = typeof(ISoftDeletable)
            .Assembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false }
                && typeof(ISoftDeletable).IsAssignableFrom(t)
            )
            .OrderBy(GetSoftDeleteCleanupOrder)
            .ThenBy(t => t.Name);

        foreach (var entityType in softDeletableTypes)
        {
            var strategyType = typeof(SoftDeleteCleanupStrategy<>).MakeGenericType(entityType);
            services.AddScoped(typeof(ISoftDeleteCleanupStrategy), strategyType);
        }
    }

    private static int GetSoftDeleteCleanupOrder(Type entityType)
    {
        var index = Array.IndexOf(SoftDeleteCleanupOrder, entityType);
        return index >= 0 ? index : int.MaxValue;
    }
}
