using Contracts.IntegrationEvents.ProductCatalog;
using Microsoft.Extensions.Logging;

namespace FileStorage.Application.Features.Files.EventHandlers;

/// <summary>
/// Handles <see cref="ProductDeletedIntegrationEvent"/> by logging the event
/// for future file orphaning logic (e.g., marking files associated with deleted products).
/// </summary>
public sealed class ProductDeletedEventHandler
{
    public static Task HandleAsync(
        ProductDeletedIntegrationEvent @event,
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

        return Task.CompletedTask;
    }
}
