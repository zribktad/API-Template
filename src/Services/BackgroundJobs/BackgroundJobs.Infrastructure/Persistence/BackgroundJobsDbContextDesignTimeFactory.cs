using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BackgroundJobs.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling to create the DbContext
/// for migration scaffolding without requiring the full runtime DI container.
/// </summary>
public sealed class BackgroundJobsDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<BackgroundJobsDbContext>
{
    public BackgroundJobsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<BackgroundJobsDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=background_jobs_db;Username=postgres;Password=postgres"
        );

        return new BackgroundJobsDbContext(optionsBuilder.Options);
    }
}
