using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Options;
using APITemplate.Infrastructure.BackgroundJobs.Services;
using APITemplate.Infrastructure.FileStorage;
using APITemplate.Infrastructure.Idempotency;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace APITemplate.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorageServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<FileStorageOptions>(configuration.SectionFor<FileStorageOptions>());
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        return services;
    }

    public static IServiceCollection AddIdempotencyStore(this IServiceCollection services)
    {
        services.AddSingleton<IIdempotencyStore>(sp =>
        {
            var multiplexer = sp.GetService<IConnectionMultiplexer>();
            if (multiplexer is not null)
                return new DistributedCacheIdempotencyStore(multiplexer);

            return new InMemoryIdempotencyStore(sp.GetRequiredService<TimeProvider>());
        });

        return services;
    }

    public static IServiceCollection AddJobServices(this IServiceCollection services)
    {
        services.AddQueueWithConsumer<
            ChannelJobQueue,
            IJobQueue,
            IJobQueueReader,
            JobProcessingBackgroundService
        >();
        return services;
    }

    public static IServiceCollection AddQueueWithConsumer<TImpl, TQueue, TReader, TService>(
        this IServiceCollection services
    )
        where TImpl : class, TQueue, TReader
        where TQueue : class
        where TReader : class
        where TService : class, IHostedService
    {
        services.AddSingleton<TImpl>();
        services.AddSingleton<TQueue>(sp => sp.GetRequiredService<TImpl>());
        services.AddSingleton<TReader>(sp => sp.GetRequiredService<TImpl>());
        services.AddHostedService<TService>();
        return services;
    }
}
