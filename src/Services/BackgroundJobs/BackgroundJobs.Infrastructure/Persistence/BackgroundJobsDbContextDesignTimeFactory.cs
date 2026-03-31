using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Infrastructure.Persistence;

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
            DesignTimeConnectionStringResolver.Resolve(
                "src/Services/BackgroundJobs/BackgroundJobs.Api",
                "DefaultConnection",
                args
            )
        );

        return new BackgroundJobsDbContext(optionsBuilder.Options);
    }
}
