using Contracts.IntegrationEvents.ProductCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Reviews.Domain.Entities;

namespace Reviews.Application.EventHandlers;

/// <summary>
/// Handles <see cref="ProductDeletedIntegrationEvent"/> by marking product projections inactive
/// and cascade soft-deleting all associated reviews.
/// </summary>
public sealed class ProductDeletedEventHandler
{
    public static async Task HandleAsync(
        ProductDeletedIntegrationEvent @event,
        DbContext dbContext,
        TimeProvider timeProvider,
        ILogger<ProductDeletedEventHandler> logger,
        CancellationToken ct
    )
    {
        // Mark projections inactive
        await dbContext
            .Set<ProductProjection>()
            .Where(p => @event.ProductIds.Contains(p.ProductId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.IsActive, false), ct);

        // Cascade soft-delete reviews
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int deletedCount = await dbContext
            .Set<ProductReview>()
            .Where(r => @event.ProductIds.Contains(r.ProductId) && !r.IsDeleted)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.IsDeleted, true)
                        .SetProperty(r => r.DeletedAtUtc, now),
                ct
            );

        logger.LogInformation(
            "Cascade soft-deleted {Count} reviews for products {ProductIds}",
            deletedCount,
            @event.ProductIds
        );
    }
}
