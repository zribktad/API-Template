using Contracts.IntegrationEvents.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Infrastructure.Persistence.SoftDelete;
using ProductProjection = Reviews.Domain.Entities.ProductProjection;
using ProductReviewEntity = Reviews.Domain.Entities.ProductReview;

namespace Reviews.Application.Features.TenantDeactivation;

/// <summary>
/// Handles <see cref="TenantDeactivatedIntegrationEvent"/> by cascade soft-deleting
/// all reviews and marking all product projections inactive for the given tenant.
/// </summary>
public sealed class TenantDeactivatedEventHandler
{
    public static async Task HandleAsync(
        TenantDeactivatedIntegrationEvent @event,
        DbContext dbContext,
        TimeProvider timeProvider,
        ILogger<TenantDeactivatedEventHandler> logger,
        CancellationToken ct
    )
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        int deletedReviews = await dbContext
            .Set<ProductReviewEntity>()
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == @event.TenantId && !r.IsDeleted)
            .BulkSoftDeleteAsync(@event.ActorId, now, ct);

        int deactivatedProducts = await dbContext
            .Set<ProductProjection>()
            .Where(p => p.TenantId == @event.TenantId && p.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.IsActive, false), ct);

        logger.LogInformation(
            "Tenant {TenantId} deactivated: soft-deleted {ReviewCount} reviews, deactivated {ProductCount} product projections",
            @event.TenantId,
            deletedReviews,
            deactivatedProducts
        );
    }
}
