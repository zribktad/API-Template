using Microsoft.EntityFrameworkCore;
using ProductCatalog.Application.Sagas;
using ProductCatalog.Domain.Entities;
using SharedKernel.Infrastructure.Persistence;

namespace ProductCatalog.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Product Catalog microservice.
/// Enforces multi-tenancy, audit stamping, soft delete, and optimistic concurrency.
/// </summary>
public sealed class ProductCatalogDbContext : TenantAuditableDbContext
{
    public ProductCatalogDbContext(
        DbContextOptions<ProductCatalogDbContext> options,
        TenantAuditableDbContextDependencies deps
    )
        : base(options, deps) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductDataLink> ProductDataLinks => Set<ProductDataLink>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductCategoryStats> ProductCategoryStats => Set<ProductCategoryStats>();
    public DbSet<ProductDeletionSaga> ProductDeletionSagas => Set<ProductDeletionSaga>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductCatalogDbContext).Assembly);

        // Global query filters for multi-tenancy and soft-delete
        modelBuilder
            .Entity<Product>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);
        modelBuilder
            .Entity<Category>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);
        modelBuilder
            .Entity<ProductDataLink>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);
    }
}
