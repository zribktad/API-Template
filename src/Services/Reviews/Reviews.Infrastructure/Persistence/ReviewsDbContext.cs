using Microsoft.EntityFrameworkCore;
using Reviews.Domain.Entities;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace Reviews.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Reviews microservice.
/// Enforces multi-tenancy, audit stamping, soft delete, and optimistic concurrency.
/// </summary>
public sealed class ReviewsDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;
    private readonly IReadOnlyCollection<ISoftDeleteCascadeRule> _softDeleteCascadeRules;
    private readonly IAuditableEntityStateManager _entityStateManager;
    private readonly ISoftDeleteProcessor _softDeleteProcessor;

    private Guid CurrentTenantId => _tenantProvider.TenantId;
    private bool HasTenant => _tenantProvider.HasTenant;

    public ReviewsDbContext(
        DbContextOptions<ReviewsDbContext> options,
        ITenantProvider tenantProvider,
        IActorProvider actorProvider,
        TimeProvider timeProvider,
        IEnumerable<ISoftDeleteCascadeRule> softDeleteCascadeRules,
        IAuditableEntityStateManager entityStateManager,
        ISoftDeleteProcessor softDeleteProcessor
    )
        : base(options)
    {
        _tenantProvider = tenantProvider;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
        _softDeleteCascadeRules = softDeleteCascadeRules.ToList();
        _entityStateManager = entityStateManager;
        _softDeleteProcessor = softDeleteProcessor;
    }

    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ProductProjection> ProductProjections => Set<ProductProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewsDbContext).Assembly);

        // Global query filters for multi-tenancy and soft-delete
        modelBuilder
            .Entity<ProductReview>()
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
