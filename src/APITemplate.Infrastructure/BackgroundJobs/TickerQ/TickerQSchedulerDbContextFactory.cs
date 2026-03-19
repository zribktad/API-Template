using APITemplate.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ;

public sealed class TickerQSchedulerDbContextFactory
    : IDesignTimeDbContextFactory<TickerQSchedulerDbContext>
{
    public TickerQSchedulerDbContext CreateDbContext(string[] args)
    {
        var configuration = Persistence.DesignTimeConfigurationHelper.BuildConfiguration();
        var connectionString = Persistence.DesignTimeConfigurationHelper.GetConnectionString(
            configuration
        );

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
