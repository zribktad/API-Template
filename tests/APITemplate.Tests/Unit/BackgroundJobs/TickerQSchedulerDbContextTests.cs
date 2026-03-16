using APITemplate.Application.Common.Options;
using APITemplate.Infrastructure.BackgroundJobs.TickerQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.BackgroundJobs;

public sealed class TickerQSchedulerDbContextTests
{
    [Fact]
    public void Model_UsesConfiguredTickerQSchemaForAllEntities()
    {
        var options = new DbContextOptionsBuilder<TickerQSchedulerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        using var dbContext = new TickerQSchedulerDbContext(
            options,
            Options.Create(
                new BackgroundJobsOptions
                {
                    TickerQ = new TickerQSchedulerOptions { SchemaName = "tickerq" },
                }
            )
        );

        dbContext.Model.GetDefaultSchema().ShouldBe("tickerq");
        dbContext
            .Model.GetEntityTypes()
            .Select(x => x.GetSchema())
            .Distinct()
            .ShouldBe(["tickerq"]);
    }
}
