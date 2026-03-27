using Microsoft.EntityFrameworkCore;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Tests;

/// <summary>
/// Minimal DbContext for handler tests. Uses SQLite in-memory so ExecuteUpdateAsync works.
/// Ignores navigation properties and complex configurations not needed for handler tests.
/// </summary>
internal sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.OwnsOne(p => p.Audit);
            entity.Ignore(p => p.Category);
            entity.Ignore(p => p.ProductDataLinks);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.OwnsOne(c => c.Audit);
            entity.Ignore(c => c.Products);
        });
    }
}
