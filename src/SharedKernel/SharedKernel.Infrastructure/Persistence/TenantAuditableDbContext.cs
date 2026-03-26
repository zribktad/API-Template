using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.EntityNormalization;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Abstract base DbContext that enforces multi-tenancy, audit stamping, soft delete,
/// and optimistic concurrency for all tenant-aware microservice contexts.
/// </summary>
public abstract class TenantAuditableDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;
    private readonly IReadOnlyCollection<ISoftDeleteCascadeRule> _softDeleteCascadeRules;
    private readonly IAuditableEntityStateManager _entityStateManager;
    private readonly ISoftDeleteProcessor _softDeleteProcessor;
    private readonly IEntityNormalizationService? _entityNormalizationService;

    protected Guid CurrentTenantId => _tenantProvider.TenantId;
    protected bool HasTenant => _tenantProvider.HasTenant;

    protected TenantAuditableDbContext(
        DbContextOptions options,
        ITenantProvider tenantProvider,
        IActorProvider actorProvider,
        TimeProvider timeProvider,
        IEnumerable<ISoftDeleteCascadeRule> softDeleteCascadeRules,
        IAuditableEntityStateManager entityStateManager,
        ISoftDeleteProcessor softDeleteProcessor,
        IEntityNormalizationService? entityNormalizationService = null
    )
        : base(options)
    {
        _tenantProvider = tenantProvider;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
        _softDeleteCascadeRules = softDeleteCascadeRules.ToList();
        _entityStateManager = entityStateManager;
        _softDeleteProcessor = softDeleteProcessor;
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
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        Guid actor = _actorProvider.ActorId;

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
                    _entityStateManager.StampAdded(
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
                    _entityStateManager.StampModified(entity, now, actor);
                    break;
                case EntityState.Deleted:
                    await _softDeleteProcessor.ProcessAsync(
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
