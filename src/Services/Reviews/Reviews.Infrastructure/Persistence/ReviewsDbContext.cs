using Microsoft.EntityFrameworkCore;
using Reviews.Domain.Entities;
using SharedKernel.Application.Context;
using SharedKernel.Infrastructure.Persistence;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace Reviews.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Reviews microservice.
/// Enforces multi-tenancy, audit stamping, soft delete, and optimistic concurrency.
/// </summary>
public sealed class ReviewsDbContext : TenantAuditableDbContext
{
    public ReviewsDbContext(
        DbContextOptions<ReviewsDbContext> options,
        ITenantProvider tenantProvider,
        IActorProvider actorProvider,
        TimeProvider timeProvider,
        IEnumerable<ISoftDeleteCascadeRule> softDeleteCascadeRules,
        IAuditableEntityStateManager entityStateManager,
        ISoftDeleteProcessor softDeleteProcessor
    )
        : base(
            options,
            tenantProvider,
            actorProvider,
            timeProvider,
            softDeleteCascadeRules,
            entityStateManager,
            softDeleteProcessor
        ) { }

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
}
