using APITemplate.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ;

/// <summary>
/// Design-time factory for <see cref="TickerQSchedulerDbContext"/>, enabling EF Core CLI
/// migration commands (<c>dotnet ef migrations add</c>) without a running host.
/// Connection string is resolved from <c>appsettings.json</c>, <c>appsettings.Development.json</c>,
/// and environment variables, falling back to a local development default.
/// </summary>
public sealed class TickerQSchedulerDbContextFactory
    : IDesignTimeDbContextFactory<TickerQSchedulerDbContext>
{
    /// <summary>Creates a configured <see cref="TickerQSchedulerDbContext"/> for tooling use.</summary>
    public TickerQSchedulerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=apitemplate;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<TickerQSchedulerDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable(
                        "__EFMigrationsHistory",
                        TickerQSchedulerOptions.DefaultSchemaName
                    )
            )
            .Options;

        return new TickerQSchedulerDbContext(options);
    }
}
