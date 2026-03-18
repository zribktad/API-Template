using APITemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APITemplate.Infrastructure.Persistence.Configurations;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        // Keep table name to match existing migration
        builder.ToTable("ExampleFiles");

        builder.HasKey(e => e.Id);
        builder.ConfigureTenantAuditable();

        builder.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);

        builder.Property(e => e.StoragePath).IsRequired().HasMaxLength(500);

        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Description).HasMaxLength(1000);

        builder
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
