using SharedKernel.Infrastructure.Queue;
using Webhooks.Application.Common.Contracts;
using Webhooks.Application.Common.DTOs;

namespace Webhooks.Infrastructure.Inbound;

/// <summary>
/// Bounded channel-based queue for inbound webhook messages, implementing both the write
/// and read marker interfaces so a single instance serves producers and the background consumer.
/// </summary>
public sealed class ChannelWebhookQueue
    : BoundedChannelQueue<InboundWebhookMessage>,
        IWebhookInboundQueue,
        IWebhookInboundQueueReader
{
    private const int DefaultCapacity = 1024;

    public ChannelWebhookQueue()
        : base(DefaultCapacity) { }
}
