using APITemplate.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ;

public sealed class TickerQSchedulerDbContextFactory
    : IDesignTimeDbContextFactory<TickerQSchedulerDbContext>
{
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

        var schemaName =
            configuration["BackgroundJobs:TickerQ:SchemaName"]
            ?? TickerQSchedulerOptions.DefaultSchemaName;

        var backgroundJobsOptions = new BackgroundJobsOptions
        {
            TickerQ = new TickerQSchedulerOptions { SchemaName = schemaName },
        };

        var options = new DbContextOptionsBuilder<TickerQSchedulerDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable(
                        "__EFMigrationsHistory",
                        backgroundJobsOptions.TickerQ.SchemaName
                    )
            )
            .Options;

        return new TickerQSchedulerDbContext(
            options,
            Microsoft.Extensions.Options.Options.Create(backgroundJobsOptions)
        );
    }
}
