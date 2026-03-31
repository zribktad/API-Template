using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling to create the DbContext
/// for migration scaffolding without requiring the full runtime DI container.
/// </summary>
public sealed class IdentityDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<IdentityDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=identity_db;Username=postgres;Password=postgres"
        );

        return new IdentityDbContext(
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
