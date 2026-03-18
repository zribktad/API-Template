using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APITemplate.Infrastructure.Webhooks;

public sealed class WebhookProcessingBackgroundService : BackgroundService
{
    private readonly ChannelWebhookQueue _queue;
    private readonly ILogger<WebhookProcessingBackgroundService> _logger;

    public WebhookProcessingBackgroundService(
        ChannelWebhookQueue queue,
        ILogger<WebhookProcessingBackgroundService> logger
    )
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var payload in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // TODO: Implement actual webhook processing logic
                _logger.LogInformation(
                    "Processing webhook: Type={EventType}, Id={EventId}",
                    payload.EventType,
                    payload.EventId
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Failed to process webhook: Type={EventType}, Id={EventId}",
                    payload.EventType,
                    payload.EventId
                );
            }
        }
    }
}
