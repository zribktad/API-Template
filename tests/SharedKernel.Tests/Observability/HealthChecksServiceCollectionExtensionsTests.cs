using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SharedKernel.Api.Extensions;
using SharedKernel.Infrastructure.Observability;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Observability;

public sealed class HealthChecksServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDragonflyHealthCheck_WhenConnectionStringMissing_DoesNotRegisterCheck()
    {
        ServiceCollection services = new();
        services.AddOptions();

        services.AddHealthChecks().AddDragonflyHealthCheck(null);

        HealthCheckServiceOptions options = services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value;

        options.Registrations.ShouldBeEmpty();
    }

    [Fact]
    public void AddPostgreSqlHealthCheck_RegistersExpectedName()
    {
        ServiceCollection services = new();
        services.AddOptions();

        services
            .AddHealthChecks()
            .AddPostgreSqlHealthCheck(
                "Host=localhost;Database=app;Username=postgres;Password=postgres"
            );

        HealthCheckServiceOptions options = services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value;

        options.Registrations.Select(x => x.Name).ShouldContain(HealthCheckNames.PostgreSql);
    }

    [Fact]
    public void AddSharedRabbitMqHealthCheck_RegistersExpectedName()
    {
        ServiceCollection services = new();
        services.AddOptions();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RabbitMQ"] = "amqp://localhost:5672",
                }
            )
            .Build();

        services.AddSingleton(configuration);
        services.AddHealthChecks().AddSharedRabbitMqHealthCheck(configuration);

        HealthCheckServiceOptions options = services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value;

        options.Registrations.Select(x => x.Name).ShouldContain(HealthCheckNames.RabbitMq);
    }
}
