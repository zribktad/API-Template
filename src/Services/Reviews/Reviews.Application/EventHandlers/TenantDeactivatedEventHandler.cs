using Contracts.IntegrationEvents.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Reviews.Domain.Entities;

namespace Reviews.Application.EventHandlers;

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

        // Cascade soft-delete all reviews for the tenant
        int deletedReviews = await dbContext
            .Set<ProductReview>()
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == @event.TenantId && !r.IsDeleted)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.IsDeleted, true)
                        .SetProperty(r => r.DeletedAtUtc, now)
                        .SetProperty(r => r.DeletedBy, @event.ActorId),
                ct
            );

        // Mark all product projections inactive for the tenant
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
