using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Common.Resilience;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Infrastructure.Webhooks;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace APITemplate.Extensions;

public static class WebhookServiceCollectionExtensions
{
    public static IServiceCollection AddIncomingWebhookServices(
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

    public static IServiceCollection AddOutgoingWebhookServices(this IServiceCollection services)
    {
        services.AddSingleton<IWebhookPayloadSigner, HmacWebhookPayloadSigner>();

        services.AddQueueWithConsumer<
            ChannelOutgoingWebhookQueue,
            IOutgoingWebhookQueue,
            IOutgoingWebhookQueueReader,
            OutgoingWebhookBackgroundService
        >();

        services
            .AddHttpClient(WebhookConstants.OutgoingHttpClientName)
            .AddResilienceHandler(
                ResiliencePipelineKeys.OutgoingWebhook,
                builder =>
                {
                    builder.AddRetry(
                        new HttpRetryStrategyOptions
                        {
                            MaxRetryAttempts = 3,
                            BackoffType = DelayBackoffType.Exponential,
                            Delay = TimeSpan.FromSeconds(2),
                            UseJitter = true,
                        }
                    );
                }
            );

        return services;
    }
}
