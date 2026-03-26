using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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
            "Host=localhost;Database=reviews_db;Username=postgres;Password=postgres"
        );

        return new ReviewsDbContext(
            optionsBuilder.Options,
            tenantProvider: null!,
            actorProvider: null!,
            timeProvider: TimeProvider.System,
            softDeleteCascadeRules: [],
            entityStateManager: null!,
            softDeleteProcessor: null!
        );
    }
}
