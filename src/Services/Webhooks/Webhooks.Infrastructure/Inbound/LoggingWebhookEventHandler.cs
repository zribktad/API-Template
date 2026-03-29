using Microsoft.Extensions.Logging;
using Webhooks.Application.Common.Contracts;

namespace Webhooks.Infrastructure.Inbound;

/// <summary>
/// Default implementation of <see cref="IWebhookEventHandler"/> that logs received events.
/// Replace with a concrete handler when business processing logic is defined.
/// </summary>
public sealed class LoggingWebhookEventHandler : IWebhookEventHandler
{
    private readonly ILogger<LoggingWebhookEventHandler> _logger;

    public LoggingWebhookEventHandler(ILogger<LoggingWebhookEventHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task HandleAsync(
        string eventType,
        string payload,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Inbound webhook received: EventType={EventType}, PayloadLength={PayloadLength}",
            eventType,
            payload.Length
        );

        return Task.CompletedTask;
    }
}
