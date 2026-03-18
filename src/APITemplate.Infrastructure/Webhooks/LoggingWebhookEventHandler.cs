using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Features.Examples.DTOs;
using Microsoft.Extensions.Logging;

namespace APITemplate.Infrastructure.Webhooks;

public sealed class LoggingWebhookEventHandler : IWebhookEventHandler
{
    private readonly ILogger<LoggingWebhookEventHandler> _logger;

    public LoggingWebhookEventHandler(ILogger<LoggingWebhookEventHandler> logger)
    {
        _logger = logger;
    }

    public string EventType => "*";

    public Task HandleAsync(WebhookPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Received webhook: Type={EventType}, Id={EventId}",
            payload.EventType,
            payload.EventId
        );

        return Task.CompletedTask;
    }
}
