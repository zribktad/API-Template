using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Options;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.BackgroundJobs;
using APITemplate.Infrastructure.BackgroundJobs.Services;
using APITemplate.Infrastructure.Email;
using APITemplate.Infrastructure.Repositories;

namespace APITemplate.Extensions;

public static class BackgroundJobsServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var section = configuration.SectionFor<BackgroundJobsOptions>();
        var options = section.Get<BackgroundJobsOptions>() ?? new BackgroundJobsOptions();
        services.Configure<BackgroundJobsOptions>(section);

        services.AddScoped<IFailedEmailRepository, FailedEmailRepository>();
        services.AddSingleton<IFailedEmailStore, FailedEmailStore>();

        services.AddScoped<ICleanupService, CleanupService>();
        services.AddScoped<IReindexService, ReindexService>();
        services.AddScoped<IEmailRetryService, EmailRetryService>();

        RegisterSoftDeleteCleanupStrategies(services);

        if (options.Cleanup.Enabled)
        {
            services.AddHostedService<CleanupBackgroundJob>();
        }

        if (options.Reindex.Enabled)
        {
            services.AddHostedService<ReindexBackgroundJob>();
        }

        if (options.EmailRetry.Enabled)
        {
            services.AddHostedService<EmailRetryBackgroundJob>();
        }

        return services;
    }

    private static void RegisterSoftDeleteCleanupStrategies(IServiceCollection services)
    {
        var softDeletableTypes = typeof(ISoftDeletable)
            .Assembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false }
                && typeof(ISoftDeletable).IsAssignableFrom(t)
            );

        foreach (var entityType in softDeletableTypes)
        {
            var strategyType = typeof(SoftDeleteCleanupStrategy<>).MakeGenericType(entityType);
            services.AddScoped(typeof(ISoftDeleteCleanupStrategy), strategyType);
        }
    }
}
