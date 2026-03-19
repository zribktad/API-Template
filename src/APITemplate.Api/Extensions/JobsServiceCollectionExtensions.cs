using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Infrastructure.BackgroundJobs.Services;

namespace APITemplate.Extensions;

public static class JobsServiceCollectionExtensions
{
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
}
