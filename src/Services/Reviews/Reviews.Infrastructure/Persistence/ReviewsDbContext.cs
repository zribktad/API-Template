using Microsoft.EntityFrameworkCore;
using Reviews.Domain.Entities;
using SharedKernel.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Reviews microservice.
/// Enforces multi-tenancy, audit stamping, soft delete, and optimistic concurrency.
/// </summary>
public sealed class ReviewsDbContext : TenantAuditableDbContext
{
    public ReviewsDbContext(
        DbContextOptions<ReviewsDbContext> options,
        TenantAuditableDbContextDependencies deps
    )
        : base(options, deps) { }

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
