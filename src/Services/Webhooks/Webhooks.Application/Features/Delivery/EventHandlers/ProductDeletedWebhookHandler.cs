using System.Text.Json;
using Contracts.IntegrationEvents.ProductCatalog;
using Microsoft.Extensions.Logging;
using Webhooks.Application.Common.Constants;
using Webhooks.Application.Common.Contracts;

namespace Webhooks.Application.Features.Delivery.EventHandlers;

/// <summary>
/// Wolverine message handler that consumes <see cref="ProductDeletedIntegrationEvent"/> from RabbitMQ
/// and delivers webhook notifications to all subscribers registered for the "product.deleted" event type.
/// </summary>
public static class ProductDeletedWebhookHandler
{
    public static async Task HandleAsync(
        ProductDeletedIntegrationEvent @event,
        IWebhookDeliveryService deliveryService,
        ILogger<ProductDeletedIntegrationEvent> logger,
        CancellationToken ct
    )
    {
        logger.LogInformation(
            "Delivering webhook for product deleted: {ProductIds}",
            string.Join(", ", @event.ProductIds)
        );

        string payload = JsonSerializer.Serialize(@event);
        await deliveryService.DeliverAsync(WebhookEventTypes.ProductDeleted, payload, ct);
    }
}
