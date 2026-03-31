using System.Text.Json;
using Contracts.IntegrationEvents.ProductCatalog;
using Microsoft.Extensions.Logging;
using Webhooks.Application.Common.Constants;
using Webhooks.Application.Common.Contracts;

namespace Webhooks.Application.Features.Delivery.EventHandlers;

/// <summary>
/// Wolverine message handler that consumes <see cref="CategoryDeletedIntegrationEvent"/> from RabbitMQ
/// and delivers webhook notifications to all subscribers registered for the "category.deleted" event type.
/// </summary>
public static class CategoryDeletedWebhookHandler
{
    public static async Task HandleAsync(
        CategoryDeletedIntegrationEvent @event,
        IWebhookDeliveryService deliveryService,
        ILogger<CategoryDeletedIntegrationEvent> logger,
        CancellationToken ct
    )
    {
        logger.LogInformation(
            "Delivering webhook for category deleted: {CategoryId}",
            @event.CategoryId
        );

        string payload = JsonSerializer.Serialize(@event);
        await deliveryService.DeliverAsync(
            WebhookEventTypes.CategoryDeleted,
            payload,
            @event.TenantId,
            ct
        );
    }
}
