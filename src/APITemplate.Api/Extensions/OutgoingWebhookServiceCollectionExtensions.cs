using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Resilience;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Infrastructure.Webhooks;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace APITemplate.Extensions;

public static class OutgoingWebhookServiceCollectionExtensions
{
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
