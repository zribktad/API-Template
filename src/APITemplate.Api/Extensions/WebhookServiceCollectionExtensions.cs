using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Options;
using APITemplate.Infrastructure.Webhooks;

namespace APITemplate.Extensions;

public static class WebhookServiceCollectionExtensions
{
    public static IServiceCollection AddWebhookServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<WebhookOptions>()
            .Bind(configuration.SectionFor<WebhookOptions>())
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IWebhookPayloadValidator, HmacWebhookPayloadValidator>();
        services.AddSingleton<ChannelWebhookQueue>();
        services.AddSingleton<IWebhookProcessingQueue>(sp =>
            sp.GetRequiredService<ChannelWebhookQueue>()
        );
        services.AddSingleton<IWebhookQueueReader>(sp =>
            sp.GetRequiredService<ChannelWebhookQueue>()
        );
        services.AddScoped<IWebhookEventHandler, LoggingWebhookEventHandler>();
        services.AddHostedService<WebhookProcessingBackgroundService>();
        return services;
    }
}
