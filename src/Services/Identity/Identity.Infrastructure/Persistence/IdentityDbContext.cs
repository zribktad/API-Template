using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.EntityNormalization;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core context for Identity & Tenancy microservice.
/// Enforces multi-tenancy, audit stamping, soft delete, and optimistic concurrency.
/// </summary>
public sealed class IdentityDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;
    private readonly IReadOnlyCollection<ISoftDeleteCascadeRule> _softDeleteCascadeRules;
    private readonly IEntityNormalizationService? _entityNormalizationService;
    private readonly IAuditableEntityStateManager _entityStateManager;
    private readonly ISoftDeleteProcessor _softDeleteProcessor;

    private Guid CurrentTenantId => _tenantProvider.TenantId;
    private bool HasTenant => _tenantProvider.HasTenant;

    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
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
        _entityNormalizationService = entityNormalizationService;
        _entityStateManager = entityStateManager;
        _softDeleteProcessor = softDeleteProcessor;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Global query filters for multi-tenancy and soft-delete
        modelBuilder
            .Entity<AppUser>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);

        modelBuilder
            .Entity<Tenant>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);

        modelBuilder
            .Entity<TenantInvitation>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);
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
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in ChangeTracker
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
