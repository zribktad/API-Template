using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Infrastructure.Persistence;

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
            DesignTimeConnectionStringResolver.Resolve(
                "src/Services/Identity/Identity.Api",
                "IdentityDb",
                args
            )
        );

        return new IdentityDbContext(
            optionsBuilder.Options,
            DesignTimeDbContextDefaults.CreateDependencies()
        );
    }
}
