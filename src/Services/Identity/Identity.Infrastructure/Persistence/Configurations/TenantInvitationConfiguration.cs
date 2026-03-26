using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Infrastructure.Persistence.Configurations;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="TenantInvitation"/> entity, including token hash and normalized email indexes.</summary>
public sealed class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(EntityTypeBuilder<TenantInvitation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.ConfigureTenantAuditable();

        builder.Property(i => i.Email).IsRequired().HasMaxLength(320);
        builder.Property(i => i.NormalizedEmail).IsRequired().HasMaxLength(320);

        builder.Property(i => i.TokenHash).IsRequired().HasMaxLength(128);

        builder
            .Property(i => i.ExpiresAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder
            .Property(i => i.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue(InvitationStatus.Pending)
            .HasSentinel((InvitationStatus)(-1));

        builder
            .HasOne(i => i.Tenant)
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.TokenHash);
        builder.HasIndex(i => new { i.TenantId, i.NormalizedEmail });
    }
}
