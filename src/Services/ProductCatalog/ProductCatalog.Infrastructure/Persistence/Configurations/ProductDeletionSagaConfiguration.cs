using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCatalog.Application.Sagas;

namespace ProductCatalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for ProductDeletionSaga persisted state.
/// Sagas are cross-service process managers and are not tenant-filtered.
/// </summary>
public sealed class ProductDeletionSagaConfiguration : IEntityTypeConfiguration<ProductDeletionSaga>
{
    public void Configure(EntityTypeBuilder<ProductDeletionSaga> builder)
    {
        builder.ToTable("ProductDeletionSagas", "sagas");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).IsRequired().HasMaxLength(64);
        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.ReviewsCascaded).IsRequired();
        builder.Property(s => s.FilesCascaded).IsRequired();

        // IReadOnlyList<Guid> is only useful at start and not needed for completion correlation/state.
        builder.Ignore(s => s.ProductIds);
    }
}
