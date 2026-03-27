using Contracts.IntegrationEvents.Identity;
using Contracts.IntegrationEvents.Sagas;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Identity.Application.EventHandlers;

/// <summary>
/// Handles <see cref="TenantDeactivatedIntegrationEvent"/> by cascade soft-deleting all
/// active users belonging to the tenant, then publishing <see cref="UsersCascadeCompleted"/>
/// so the <c>TenantDeactivationSaga</c> can track progress.
/// </summary>
public sealed class TenantDeactivatedEventHandler
{
    public static async Task HandleAsync(
        TenantDeactivatedIntegrationEvent @event,
        DbContext dbContext,
        IMessageBus bus,
        TimeProvider timeProvider,
        ILogger<TenantDeactivatedEventHandler> logger,
        CancellationToken ct
    )
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        int deactivatedCount = await dbContext
            .Set<AppUser>()
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == @event.TenantId && !u.IsDeleted)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(u => u.IsDeleted, true)
                        .SetProperty(u => u.DeletedAtUtc, now)
                        .SetProperty(u => u.DeletedBy, @event.ActorId),
                ct
            );

        // ExecuteUpdateAsync bypasses the change tracker; clear stale cached entities
        // so any subsequent reads in the same unit of work see the updated state.
        dbContext.ChangeTracker.Clear();

        logger.LogInformation(
            "Tenant {TenantId} deactivated: soft-deleted {UserCount} users",
            @event.TenantId,
            deactivatedCount
        );

        await bus.PublishAsync(
            new UsersCascadeCompleted(@event.CorrelationId, @event.TenantId, deactivatedCount)
        );
    }
}
