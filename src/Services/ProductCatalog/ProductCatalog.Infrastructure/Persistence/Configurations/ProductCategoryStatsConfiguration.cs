using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCatalog.Domain.Entities;

namespace ProductCatalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Registers <see cref="ProductCategoryStats"/> as a keyless entity.
/// </summary>
public sealed class ProductCategoryStatsConfiguration
    : IEntityTypeConfiguration<ProductCategoryStats>
{
    public void Configure(EntityTypeBuilder<ProductCategoryStats> builder)
    {
        builder.HasNoKey();
        builder.ToTable("ProductCategoryStats", t => t.ExcludeFromMigrations());
    }
}
