using APITemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APITemplate.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.ConfigureTenantAuditable();

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);

        builder
            .Property(t => t.ExpiresAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.IsUsed).IsRequired().HasDefaultValue(false);

        builder
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TokenHash);
        builder.HasIndex(t => t.UserId);
    }
}
