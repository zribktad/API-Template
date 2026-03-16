using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Options;
using APITemplate.Extensions;
using APITemplate.Infrastructure.BackgroundJobs.TickerQ;
using APITemplate.Infrastructure.BackgroundJobs.TickerQ.Coordination;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.BackgroundJobs;

public sealed class BackgroundJobsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBackgroundJobs_WhenTickerQEnabled_RegistersTickerQInfrastructureAndRemovesLegacyHostedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=test;Username=test;Password=test",
                ["Dragonfly:ConnectionString"] = "localhost:6379",
                ["BackgroundJobs:TickerQ:Enabled"] = "true",
                ["BackgroundJobs:TickerQ:SchemaName"] = "tickerq",
                ["BackgroundJobs:Cleanup:Enabled"] = "true",
                ["BackgroundJobs:Cleanup:Cron"] = "0 * * * *",
                ["BackgroundJobs:Reindex:Enabled"] = "true",
                ["BackgroundJobs:Reindex:Cron"] = "0 */6 * * *",
                ["BackgroundJobs:EmailRetry:Enabled"] = "true",
                ["BackgroundJobs:EmailRetry:Cron"] = "*/15 * * * *",
            }
        );

        services.AddBackgroundJobs(configuration);

        services.ShouldContain(x => x.ServiceType == typeof(TickerQSchedulerDbContext));
        services.ShouldContain(x => x.ServiceType == typeof(TickerQRecurringJobRegistrar));
        services.ShouldContain(x => x.ServiceType == typeof(IDistributedJobCoordinator));
        services.ShouldContain(x => x.ServiceType == typeof(IExternalIntegrationSyncService));
        services
            .Count(x => x.ServiceType == typeof(IRecurringBackgroundJobRegistration))
            .ShouldBe(4);
        services.ShouldContain(x => x.ServiceType == typeof(IFailedEmailStore));
        services.ShouldContain(x => x.ServiceType == typeof(IEmailRetryService));
        services.ShouldContain(x => x.ServiceType == typeof(ICleanupService));
        services.ShouldContain(x => x.ServiceType == typeof(IReindexService));
        services.ShouldNotContain(x =>
            x.ServiceType == typeof(IHostedService)
            && x.ImplementationType != null
            && (
                x.ImplementationType.Name == "CleanupBackgroundJob"
                || x.ImplementationType.Name == "ReindexBackgroundJob"
                || x.ImplementationType.Name == "EmailRetryBackgroundJob"
                || x.ImplementationType.Name == "PeriodicBackgroundJob"
            )
        );
    }

    [Fact]
    public void AddBackgroundJobs_WhenTickerQEnabledWithoutDragonflyConnection_Throws()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=test;Username=test;Password=test",
                ["BackgroundJobs:TickerQ:Enabled"] = "true",
            }
        );

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddBackgroundJobs(configuration)
        );

        ex.Message.ShouldContain("Dragonfly:ConnectionString");
    }

    [Fact]
    public void AddBackgroundJobs_BindsTickerQOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=test;Username=test;Password=test",
                ["Dragonfly:ConnectionString"] = "localhost:6379",
                ["BackgroundJobs:TickerQ:Enabled"] = "true",
                ["BackgroundJobs:TickerQ:FailClosed"] = "true",
                ["BackgroundJobs:TickerQ:SchemaName"] = "tickerq",
                ["BackgroundJobs:TickerQ:InstanceNamePrefix"] = "ApiTemplate",
                ["BackgroundJobs:TickerQ:CoordinationConnection"] = "Dragonfly",
                ["BackgroundJobs:ExternalSync:Cron"] = "0 */12 * * *",
                ["BackgroundJobs:Cleanup:Cron"] = "5 * * * *",
                ["BackgroundJobs:Reindex:Cron"] = "0 */4 * * *",
                ["BackgroundJobs:EmailRetry:Cron"] = "*/5 * * * *",
                ["BackgroundJobs:EmailRetry:ClaimLeaseMinutes"] = "9",
            }
        );

        services.AddBackgroundJobs(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<BackgroundJobsOptions>>()
            .Value;

        options.TickerQ.Enabled.ShouldBeTrue();
        options.TickerQ.FailClosed.ShouldBeTrue();
        options.TickerQ.SchemaName.ShouldBe("tickerq");
        options.TickerQ.InstanceNamePrefix.ShouldBe("ApiTemplate");
        options.TickerQ.CoordinationConnection.ShouldBe("Dragonfly");
        options.ExternalSync.Cron.ShouldBe("0 */12 * * *");
        options.Cleanup.Cron.ShouldBe("5 * * * *");
        options.Reindex.Cron.ShouldBe("0 */4 * * *");
        options.EmailRetry.Cron.ShouldBe("*/5 * * * *");
        options.EmailRetry.ClaimLeaseMinutes.ShouldBe(9);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
