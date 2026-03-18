using APITemplate.Application.Features.Examples.DTOs;

namespace APITemplate.Application.Common.Contracts;

public interface IWebhookEventHandler
{
    string EventType { get; }
    Task HandleAsync(WebhookPayload payload, CancellationToken ct = default);
}
