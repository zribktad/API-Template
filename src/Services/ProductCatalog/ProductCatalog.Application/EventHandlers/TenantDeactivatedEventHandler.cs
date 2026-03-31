using Contracts.IntegrationEvents.Identity;
using Contracts.IntegrationEvents.Sagas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductCatalog.Domain.Entities;
using SharedKernel.Infrastructure.Persistence.SoftDelete;
using Wolverine;

namespace ProductCatalog.Application.EventHandlers;

/// <summary>
/// Handles <see cref="TenantDeactivatedIntegrationEvent"/> by cascade soft-deleting all
/// products and categories belonging to the tenant, then publishing
/// <see cref="ProductsCascadeCompleted"/> and <see cref="CategoriesCascadeCompleted"/>
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

        int deletedProducts = await dbContext
            .Set<Product>()
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == @event.TenantId && !p.IsDeleted)
            .BulkSoftDeleteAsync(@event.ActorId, now, ct);

        int deletedCategories = await dbContext
            .Set<Category>()
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == @event.TenantId && !c.IsDeleted)
            .BulkSoftDeleteAsync(@event.ActorId, now, ct);

        // ExecuteUpdateAsync bypasses the change tracker; clear stale cached entities
        // so any subsequent reads in the same unit of work see the updated state.
        dbContext.ChangeTracker.Clear();

        logger.LogInformation(
            "Tenant {TenantId} deactivated: soft-deleted {ProductCount} products, {CategoryCount} categories",
            @event.TenantId,
            deletedProducts,
            deletedCategories
        );

        await bus.PublishAsync(
            new ProductsCascadeCompleted(@event.CorrelationId, @event.TenantId, deletedProducts)
        );
        await bus.PublishAsync(
            new CategoriesCascadeCompleted(@event.CorrelationId, @event.TenantId, deletedCategories)
        );
    }
}
