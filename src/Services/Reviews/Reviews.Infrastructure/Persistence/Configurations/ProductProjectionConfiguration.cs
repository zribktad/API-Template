using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="ProductProjection"/> read model.</summary>
public sealed class ProductProjectionConfiguration : IEntityTypeConfiguration<ProductProjection>
{
    public void Configure(EntityTypeBuilder<ProductProjection> builder)
    {
        builder.HasKey(p => p.ProductId);

        builder.Property(p => p.TenantId).IsRequired();

        builder.Property(p => p.Name).HasMaxLength(500).IsRequired();

        builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(p => p.TenantId);
    }
}
