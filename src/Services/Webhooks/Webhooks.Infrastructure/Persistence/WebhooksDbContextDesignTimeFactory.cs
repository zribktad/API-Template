using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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
            "Host=localhost;Database=webhooks_db;Username=postgres;Password=postgres"
        );

        return new WebhooksDbContext(optionsBuilder.Options);
    }
}
