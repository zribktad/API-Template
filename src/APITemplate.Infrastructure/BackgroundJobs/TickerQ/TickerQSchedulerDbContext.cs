using APITemplate.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.Utilities.Entities;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ;

public sealed class TickerQSchedulerDbContext : TickerQDbContext<TimeTickerEntity, CronTickerEntity>
{
    private readonly string _schemaName;

    public TickerQSchedulerDbContext(
        DbContextOptions<TickerQSchedulerDbContext> options,
        IOptions<BackgroundJobsOptions>? backgroundJobsOptions = null
    )
        : base(options)
    {
        _schemaName =
            backgroundJobsOptions?.Value.TickerQ.SchemaName
            ?? TickerQSchedulerOptions.DefaultSchemaName;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schemaName);
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetSchema(_schemaName);
        }
    }
}
