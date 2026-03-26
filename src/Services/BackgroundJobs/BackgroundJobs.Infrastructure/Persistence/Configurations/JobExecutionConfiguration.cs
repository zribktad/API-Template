using BackgroundJobs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackgroundJobs.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="JobExecution"/> entity.
/// </summary>
public sealed class JobExecutionConfiguration : IEntityTypeConfiguration<JobExecution>
{
    public void Configure(EntityTypeBuilder<JobExecution> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.JobType).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.Parameters).HasColumnType("text");

        builder.Property(e => e.CallbackUrl).HasMaxLength(2048);

        builder.Property(e => e.ResultPayload).HasColumnType("text");

        builder.Property(e => e.ErrorMessage).HasColumnType("text");

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.SubmittedAtUtc);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.OwnsOne(e => e.Audit);
    }
}
