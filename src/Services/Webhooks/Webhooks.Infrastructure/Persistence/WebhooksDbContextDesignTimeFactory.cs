using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Infrastructure.Persistence;

namespace Webhooks.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling to create the DbContext
/// for migration scaffolding without requiring the full runtime DI container.
/// </summary>
public sealed class WebhooksDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<WebhooksDbContext>
{
    public WebhooksDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<WebhooksDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            DesignTimeConnectionStringResolver.Resolve(
                "src/Services/Webhooks/Webhooks.Api",
                "DefaultConnection",
                args
            )
        );

        return new WebhooksDbContext(optionsBuilder.Options);
    }
}
