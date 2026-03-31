using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Infrastructure.Persistence.EntityNormalization;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Abstract base DbContext that enforces multi-tenancy, audit stamping, soft delete,
/// and optimistic concurrency for all tenant-aware microservice contexts.
/// </summary>
public abstract class TenantAuditableDbContext : DbContext
{
    private readonly TenantAuditableDbContextDependencies _deps;
    private readonly IReadOnlyList<ISoftDeleteCascadeRule> _softDeleteCascadeRules;
    private readonly IEntityNormalizationService? _entityNormalizationService;

    protected Guid CurrentTenantId => _deps.TenantProvider.TenantId;
    protected bool HasTenant => _deps.TenantProvider.HasTenant;

    protected TenantAuditableDbContext(
        DbContextOptions options,
        TenantAuditableDbContextDependencies deps,
        IEntityNormalizationService? entityNormalizationService = null
    )
        : base(options)
    {
        _deps = deps;
        _softDeleteCascadeRules = deps.SoftDeleteCascadeRules.ToList();
        _entityNormalizationService = entityNormalizationService;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new NotSupportedException(
            "Use SaveChangesAsync to avoid deadlocks. All paths should go through IUnitOfWork.CommitAsync()."
        );
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        await ApplyEntityAuditingAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task ApplyEntityAuditingAsync(CancellationToken cancellationToken)
    {
        DateTime now = _deps.TimeProvider.GetUtcNow().UtcDateTime;
        Guid actor = _deps.ActorProvider.ActorId;

        foreach (
            EntityEntry entry in ChangeTracker
                .Entries()
                .Where(e => e.Entity is IAuditableTenantEntity)
                .ToList()
        )
        {
            IAuditableTenantEntity entity = (IAuditableTenantEntity)entry.Entity;
            switch (entry.State)
            {
                case EntityState.Added:
                    _entityNormalizationService?.Normalize(entity);
                    _deps.EntityStateManager.StampAdded(
                        entry,
                        entity,
                        now,
                        actor,
                        HasTenant,
                        CurrentTenantId
                    );
                    break;
                case EntityState.Modified:
                    _entityNormalizationService?.Normalize(entity);
                    _deps.EntityStateManager.StampModified(entity, now, actor);
                    break;
                case EntityState.Deleted:
                    await _deps.SoftDeleteProcessor.ProcessAsync(
                        this,
                        entry,
                        entity,
                        now,
                        actor,
                        _softDeleteCascadeRules,
                        cancellationToken
                    );
                    break;
            }
        }
    }
}
