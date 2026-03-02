using APITemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace APITemplate.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Keyless entity — no backing table.
    /// Materialised only via <c>FromSql()</c> when calling the stored procedure.
    /// </summary>
    public DbSet<ProductCategoryStats> ProductCategoryStats => Set<ProductCategoryStats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
