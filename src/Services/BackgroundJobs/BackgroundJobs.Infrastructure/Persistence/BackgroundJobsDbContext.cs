using BackgroundJobs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackgroundJobs.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext scoped to the BackgroundJobs microservice, managing only job-related entities.
/// </summary>
public sealed class BackgroundJobsDbContext : DbContext
{
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();

    public BackgroundJobsDbContext(DbContextOptions<BackgroundJobsDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BackgroundJobsDbContext).Assembly);
    }
}
