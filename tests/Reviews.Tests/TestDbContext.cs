using Microsoft.EntityFrameworkCore;
using Reviews.Domain.Entities;

namespace Reviews.Tests;

/// <summary>
/// Minimal DbContext for in-memory testing of handlers that use DbContext.Set&lt;T&gt;() directly.
/// </summary>
internal sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<ProductProjection> ProductProjections => Set<ProductProjection>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductProjection>().HasKey(p => p.ProductId);

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.OwnsOne(r => r.Audit);
        });
    }
}
