using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductCatalog.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling to create the DbContext
/// for migration scaffolding without requiring the full runtime DI container.
/// </summary>
public sealed class ProductCatalogDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<ProductCatalogDbContext>
{
    public ProductCatalogDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ProductCatalogDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=product_catalog_db;Username=postgres;Password=postgres"
        );

        return new ProductCatalogDbContext(
            optionsBuilder.Options,
            tenantProvider: null!,
            actorProvider: null!,
            timeProvider: TimeProvider.System,
            softDeleteCascadeRules: [],
            entityStateManager: null!,
            softDeleteProcessor: null!
        );
    }
}
