using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APITemplate.Infrastructure.Webhooks;

public sealed class WebhookProcessingBackgroundService : BackgroundService
{
    private readonly IWebhookQueueReader _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookProcessingBackgroundService> _logger;

    public WebhookProcessingBackgroundService(
        IWebhookQueueReader queue,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookProcessingBackgroundService> logger
    )
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var payload in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var handlers = scope.ServiceProvider.GetRequiredService<
                    IEnumerable<IWebhookEventHandler>
                >();

                var handled = false;
                foreach (var handler in handlers)
                {
                    if (handler.EventType == "*" || handler.EventType == payload.EventType)
                    {
                        await handler.HandleAsync(payload, stoppingToken);
                        handled = true;
                    }
                }

                if (!handled)
                {
                    _logger.LogWarning(
                        "No handler registered for webhook event type '{EventType}' (Id={EventId})",
                        payload.EventType,
                        payload.EventId
                    );
                }
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
