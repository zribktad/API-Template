using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling to create the DbContext
/// for migration scaffolding without requiring the full runtime DI container.
/// </summary>
public sealed class ReviewsDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<ReviewsDbContext>
{
    public ReviewsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ReviewsDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            DesignTimeConnectionStringResolver.Resolve(
                "src/Services/Reviews/Reviews.Api",
                "ReviewsDb",
                args
            )
        );

        return new ReviewsDbContext(
            optionsBuilder.Options,
            DesignTimeDbContextDefaults.CreateDependencies()
        );
    }
}
