using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Infrastructure.BackgroundJobs.Services;

namespace APITemplate.Infrastructure.Webhooks;

public sealed class ChannelWebhookQueue
    : BoundedChannelQueue<WebhookPayload>,
        IWebhookProcessingQueue
{
    private const int DefaultCapacity = 500;

    public ChannelWebhookQueue()
        : base(DefaultCapacity) { }
}
