using APITemplate.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.Utilities.Entities;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ;

public sealed class TickerQSchedulerDbContext : TickerQDbContext<TimeTickerEntity, CronTickerEntity>
{
    public TickerQSchedulerDbContext(DbContextOptions<TickerQSchedulerDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(TickerQSchedulerOptions.DefaultSchemaName);
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetSchema(TickerQSchedulerOptions.DefaultSchemaName);
        }
    }
}
