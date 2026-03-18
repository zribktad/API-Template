using Microsoft.Extensions.Hosting;

namespace APITemplate.Extensions;

public static class QueueServiceCollectionExtensions
{
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
