using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Infrastructure.Persistence;

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
            DesignTimeConnectionStringResolver.Resolve(
                "src/Services/ProductCatalog/ProductCatalog.Api",
                "ProductCatalogDb",
                args
            )
        );

        return new ProductCatalogDbContext(
            optionsBuilder.Options,
            DesignTimeDbContextDefaults.CreateDependencies()
        );
    }
}
