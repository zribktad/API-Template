using Contracts.IntegrationEvents.ProductCatalog;
using Contracts.IntegrationEvents.Sagas;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace FileStorage.Application.Features.Files.EventHandlers;

/// <summary>
/// Handles <see cref="ProductDeletedIntegrationEvent"/> by logging the event
/// for future file orphaning logic (e.g., marking files associated with deleted products).
/// </summary>
public sealed class ProductDeletedEventHandler
{
    public static async Task HandleAsync(
        ProductDeletedIntegrationEvent @event,
        IMessageBus bus,
        ILogger<ProductDeletedEventHandler> logger,
        CancellationToken ct
    )
    {
        logger.LogInformation(
            "Received ProductDeletedIntegrationEvent for {Count} product(s) in tenant {TenantId}. "
                + "File orphaning logic can be implemented here.",
            @event.ProductIds.Count,
            @event.TenantId
        );

        await bus.PublishAsync(new FilesCascadeCompleted(@event.CorrelationId, 0));
    }
}
