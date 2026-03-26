using Contracts.IntegrationEvents.ProductCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Reviews.Domain.Entities;

namespace Reviews.Application.EventHandlers;

/// <summary>
/// Handles <see cref="ProductCreatedIntegrationEvent"/> by creating or reactivating
/// a local <see cref="ProductProjection"/> for product existence checks.
/// </summary>
public sealed class ProductCreatedEventHandler
{
    public static async Task HandleAsync(
        ProductCreatedIntegrationEvent @event,
        DbContext dbContext,
        ILogger<ProductCreatedEventHandler> logger,
        CancellationToken ct
    )
    {
        ProductProjection? existing = await dbContext
            .Set<ProductProjection>()
            .FindAsync([@event.ProductId], ct);

        if (existing is not null)
        {
            existing.Name = @event.Name;
            existing.IsActive = true;
            existing.TenantId = @event.TenantId;
            logger.LogInformation(
                "Reactivated ProductProjection for product {ProductId}",
                @event.ProductId
            );
        }
        else
        {
            ProductProjection projection = new()
            {
                ProductId = @event.ProductId,
                TenantId = @event.TenantId,
                Name = @event.Name,
                IsActive = true,
            };
            dbContext.Set<ProductProjection>().Add(projection);
            logger.LogInformation(
                "Created ProductProjection for product {ProductId}",
                @event.ProductId
            );
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
