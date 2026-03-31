using System.Text.Json;
using Contracts.IntegrationEvents.ProductCatalog;
using Microsoft.Extensions.Logging;
using Webhooks.Application.Common.Constants;
using Webhooks.Application.Common.Contracts;

namespace Webhooks.Application.Features.Delivery.EventHandlers;

/// <summary>
/// Wolverine message handler that consumes <see cref="ProductCreatedIntegrationEvent"/> from RabbitMQ
/// and delivers webhook notifications to all subscribers registered for the "product.created" event type.
/// </summary>
public static class ProductCreatedWebhookHandler
{
    public static async Task HandleAsync(
        ProductCreatedIntegrationEvent @event,
        IWebhookDeliveryService deliveryService,
        ILogger<ProductCreatedIntegrationEvent> logger,
        CancellationToken ct
    )
    {
        logger.LogInformation(
            "Delivering webhook for product created: {ProductId}",
            @event.ProductId
        );

        string payload = JsonSerializer.Serialize(@event);
        await deliveryService.DeliverAsync(
            WebhookEventTypes.ProductCreated,
            payload,
            @event.TenantId,
            ct
        );
    }
}
