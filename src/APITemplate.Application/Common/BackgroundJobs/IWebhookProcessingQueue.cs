using APITemplate.Application.Features.Examples.DTOs;

namespace APITemplate.Application.Common.BackgroundJobs;

public interface IWebhookProcessingQueue
{
    ValueTask EnqueueAsync(WebhookPayload payload, CancellationToken ct = default);
}
