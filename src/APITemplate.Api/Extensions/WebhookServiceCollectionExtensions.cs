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
        services.AddQueueWithConsumer<
            ChannelWebhookQueue,
            IWebhookProcessingQueue,
            IWebhookQueueReader,
            WebhookProcessingBackgroundService
        >();
        services.AddScoped<IWebhookEventHandler, LoggingWebhookEventHandler>();
        return services;
    }
}
