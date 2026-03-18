using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Infrastructure.BackgroundJobs.Services;

namespace APITemplate.Extensions;

public static class JobsServiceCollectionExtensions
{
    public static IServiceCollection AddJobServices(this IServiceCollection services)
    {
        services.AddSingleton<ChannelJobQueue>();
        services.AddSingleton<IJobQueue>(sp => sp.GetRequiredService<ChannelJobQueue>());
        services.AddHostedService<JobProcessingBackgroundService>();
        return services;
    }
}
