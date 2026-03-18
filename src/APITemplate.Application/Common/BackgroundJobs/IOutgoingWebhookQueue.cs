using APITemplate.Application.Features.Examples.DTOs;

namespace APITemplate.Application.Common.BackgroundJobs;

public interface IOutgoingWebhookQueue
{
    ValueTask EnqueueAsync(OutgoingWebhookItem item, CancellationToken ct = default);
}

public interface IOutgoingWebhookQueueReader : IQueueReader<OutgoingWebhookItem>;
