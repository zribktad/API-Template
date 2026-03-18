using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Infrastructure.BackgroundJobs.Services;

namespace APITemplate.Infrastructure.Webhooks;

public sealed class ChannelOutgoingWebhookQueue
    : BoundedChannelQueue<OutgoingWebhookItem>,
        IOutgoingWebhookQueue,
        IOutgoingWebhookQueueReader
{
    private const int DefaultCapacity = 500;

    public ChannelOutgoingWebhookQueue()
        : base(DefaultCapacity) { }
}
