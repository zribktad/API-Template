using System.Text.Json;
using Contracts.IntegrationEvents.Reviews;
using Microsoft.Extensions.Logging;
using Webhooks.Application.Common.Constants;
using Webhooks.Application.Common.Contracts;

namespace Webhooks.Application.Features.Delivery.EventHandlers;

/// <summary>
/// Wolverine message handler that consumes <see cref="ReviewCreatedIntegrationEvent"/> from RabbitMQ
/// and delivers webhook notifications to all subscribers registered for the "review.created" event type.
/// </summary>
public static class ReviewCreatedWebhookHandler
{
    public static async Task HandleAsync(
        ReviewCreatedIntegrationEvent @event,
        IWebhookDeliveryService deliveryService,
        ILogger<ReviewCreatedIntegrationEvent> logger,
        CancellationToken ct
    )
    {
        logger.LogInformation(
            "Delivering webhook for review created: {ReviewId} on product {ProductId}",
            @event.ReviewId,
            @event.ProductId
        );

        string payload = JsonSerializer.Serialize(@event);
        await deliveryService.DeliverAsync(WebhookEventTypes.ReviewCreated, payload, ct);
    }
}
