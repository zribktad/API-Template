using APITemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace APITemplate.Infrastructure.Persistence.Configurations;

public sealed class FailedEmailConfiguration : IEntityTypeConfiguration<FailedEmail>
{
    public void Configure(EntityTypeBuilder<FailedEmail> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.To).IsRequired().HasMaxLength(320);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(500);
        builder.Property(e => e.HtmlBody).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.TemplateName).HasMaxLength(100);

        builder
            .Property(e => e.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.LastAttemptAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(e => new
        {
            e.IsDeadLettered,
            e.RetryCount,
            e.LastAttemptAtUtc,
        });
    }
}
