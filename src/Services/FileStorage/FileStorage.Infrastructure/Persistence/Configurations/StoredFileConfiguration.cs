using FileStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Infrastructure.Persistence.Configurations;

namespace FileStorage.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="StoredFile"/> entity, mapped to the <c>StoredFiles</c> table.</summary>
public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        builder.HasKey(e => e.Id);
        builder.ConfigureTenantAuditable();

        builder.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);

        builder.Property(e => e.StoragePath).IsRequired().HasMaxLength(500);

        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Description).HasMaxLength(1000);
    }
}
