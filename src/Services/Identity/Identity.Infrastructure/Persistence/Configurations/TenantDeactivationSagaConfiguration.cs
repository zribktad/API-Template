using Identity.Application.Sagas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for TenantDeactivationSaga persisted state.
/// Sagas are orchestration state and are not tenant-filtered.
/// </summary>
public sealed class TenantDeactivationSagaConfiguration
    : IEntityTypeConfiguration<TenantDeactivationSaga>
{
    public void Configure(EntityTypeBuilder<TenantDeactivationSaga> builder)
    {
        builder.ToTable("TenantDeactivationSagas", "sagas");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).IsRequired().HasMaxLength(64);
        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.UsersCascaded).IsRequired();
        builder.Property(s => s.ProductsCascaded).IsRequired();
        builder.Property(s => s.CategoriesCascaded).IsRequired();
    }
}
