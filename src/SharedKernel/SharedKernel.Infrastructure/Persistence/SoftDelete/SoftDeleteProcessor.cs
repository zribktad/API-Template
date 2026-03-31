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
        HashSet<IAuditableTenantEntity> visited = new(ReferenceEqualityComparer.Instance);
        return SoftDeleteWithRulesAsync(
            dbContext,
            entry,
            entity,
            now,
            actor,
            softDeleteCascadeRules,
            visited,
            cancellationToken
        );
    }

    private async Task SoftDeleteWithRulesAsync(
        DbContext dbContext,
        EntityEntry entry,
        IAuditableTenantEntity entity,
        DateTime now,
        Guid actor,
        IReadOnlyCollection<ISoftDeleteCascadeRule> softDeleteCascadeRules,
        HashSet<IAuditableTenantEntity> visited,
        CancellationToken cancellationToken
    )
    {
        if (!visited.Add(entity))
            return;

        _stateManager.MarkSoftDeleted(entry, entity, now, actor);

        foreach (
            ISoftDeleteCascadeRule rule in softDeleteCascadeRules.Where(r => r.CanHandle(entity))
        )
        {
            IReadOnlyCollection<IAuditableTenantEntity> dependents = await rule.GetDependentsAsync(
                dbContext,
                entity,
                cancellationToken
            );
            foreach (IAuditableTenantEntity dependent in dependents)
            {
                if (dependent.IsDeleted || dependent.TenantId != entity.TenantId)
                    continue;

                EntityEntry dependentEntry = dbContext.Entry(dependent);
                await SoftDeleteWithRulesAsync(
                    dbContext,
                    dependentEntry,
                    dependent,
                    now,
                    actor,
                    softDeleteCascadeRules,
                    visited,
                    cancellationToken
                );
            }
        }
    }
}
