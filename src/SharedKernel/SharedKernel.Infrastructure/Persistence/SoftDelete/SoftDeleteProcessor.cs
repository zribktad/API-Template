using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Infrastructure.Persistence.Auditing;

namespace SharedKernel.Infrastructure.Persistence.SoftDelete;

/// <summary>
/// Default implementation of <see cref="ISoftDeleteProcessor"/> that recursively soft-deletes
/// an entity and all dependents surfaced by cascade rules, guarding against cycles via a visited set.
/// </summary>
public sealed class SoftDeleteProcessor : ISoftDeleteProcessor
{
    private readonly IAuditableEntityStateManager _stateManager;

    public SoftDeleteProcessor(IAuditableEntityStateManager stateManager)
    {
        _stateManager = stateManager;
    }

    /// <inheritdoc />
    public Task ProcessAsync(
        DbContext dbContext,
        EntityEntry entry,
        IAuditableTenantEntity entity,
        DateTime now,
        Guid actor,
        IReadOnlyCollection<ISoftDeleteCascadeRule> softDeleteCascadeRules,
        CancellationToken cancellationToken
    )
    {
        SoftDeleteOperationContext ctx = new(
            dbContext,
            now,
            actor,
            softDeleteCascadeRules,
            new HashSet<IAuditableTenantEntity>(ReferenceEqualityComparer.Instance)
        );
        return SoftDeleteWithRulesAsync(ctx, entry, entity, cancellationToken);
    }

    private async Task SoftDeleteWithRulesAsync(
        SoftDeleteOperationContext ctx,
        EntityEntry entry,
        IAuditableTenantEntity entity,
        CancellationToken cancellationToken
    )
    {
        if (!ctx.Visited.Add(entity))
            return;

        _stateManager.MarkSoftDeleted(entry, entity, ctx.Now, ctx.Actor);

        foreach (ISoftDeleteCascadeRule rule in ctx.Rules.Where(r => r.CanHandle(entity)))
        {
            IReadOnlyCollection<IAuditableTenantEntity> dependents = await rule.GetDependentsAsync(
                ctx.DbContext,
                entity,
                cancellationToken
            );
            foreach (IAuditableTenantEntity dependent in dependents)
            {
                if (dependent.IsDeleted || dependent.TenantId != entity.TenantId)
                    continue;

                EntityEntry dependentEntry = ctx.DbContext.Entry(dependent);
                await SoftDeleteWithRulesAsync(ctx, dependentEntry, dependent, cancellationToken);
            }
        }
    }

    private sealed record SoftDeleteOperationContext(
        DbContext DbContext,
        DateTime Now,
        Guid Actor,
        IReadOnlyCollection<ISoftDeleteCascadeRule> Rules,
        HashSet<IAuditableTenantEntity> Visited
    );
}
