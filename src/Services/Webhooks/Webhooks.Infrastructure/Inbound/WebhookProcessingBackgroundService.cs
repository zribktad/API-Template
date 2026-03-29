using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Infrastructure.Queue;
using Webhooks.Application.Common.Contracts;
using Webhooks.Application.Common.DTOs;

namespace Webhooks.Infrastructure.Inbound;

/// <summary>
/// Background service that continuously drains the inbound webhook queue and dispatches
/// each message to the registered <see cref="IWebhookEventHandler"/>.
/// </summary>
public sealed class WebhookProcessingBackgroundService
    : QueueConsumerBackgroundService<InboundWebhookMessage>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookProcessingBackgroundService> _logger;

    public WebhookProcessingBackgroundService(
        IWebhookInboundQueueReader queue,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookProcessingBackgroundService> logger
    )
        : base(queue)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ProcessItemAsync(InboundWebhookMessage item, CancellationToken ct)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        IWebhookEventHandler handler =
            scope.ServiceProvider.GetRequiredService<IWebhookEventHandler>();
        await handler.HandleAsync(item.EventType, item.Payload, ct);
    }

    /// <inheritdoc />
    protected override Task HandleErrorAsync(
        InboundWebhookMessage item,
        Exception ex,
        CancellationToken ct
    )
    {
        _logger.LogError(
            ex,
            "Failed to process inbound webhook: EventType={EventType}, ReceivedAt={ReceivedAtUtc}",
            item.EventType,
            item.ReceivedAtUtc
        );

        return Task.CompletedTask;
    }
}
