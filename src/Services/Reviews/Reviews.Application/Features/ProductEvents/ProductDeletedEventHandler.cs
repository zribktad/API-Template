using Contracts.IntegrationEvents.ProductCatalog;
using Contracts.IntegrationEvents.Sagas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Infrastructure.Persistence.SoftDelete;
using Wolverine;
using ProductProjection = Reviews.Domain.Entities.ProductProjection;
using ProductReviewEntity = Reviews.Domain.Entities.ProductReview;

namespace Reviews.Application.Features.ProductEvents;

/// <summary>
/// Handles <see cref="ProductDeletedIntegrationEvent"/> by marking product projections inactive
/// and cascade soft-deleting all associated reviews.
/// </summary>
public sealed class ProductDeletedEventHandler
{
    public static async Task HandleAsync(
        ProductDeletedIntegrationEvent @event,
        DbContext dbContext,
        IMessageBus bus,
        TimeProvider timeProvider,
        ILogger<ProductDeletedEventHandler> logger,
        CancellationToken ct
    )
    {
        await dbContext
            .Set<ProductProjection>()
            .Where(p => @event.ProductIds.Contains(p.ProductId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.IsActive, false), ct);

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int deletedCount = await dbContext
            .Set<ProductReviewEntity>()
            .Where(r => @event.ProductIds.Contains(r.ProductId) && !r.IsDeleted)
            .BulkSoftDeleteAsync(actorId: null, now, ct);

        logger.LogInformation(
            "Cascade soft-deleted {Count} reviews for products {ProductIds}",
            deletedCount,
            @event.ProductIds
        );

        await bus.PublishAsync(new ReviewsCascadeCompleted(@event.CorrelationId, deletedCount));
    }
}
