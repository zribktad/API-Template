using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedKernel.Messaging.Conventions;
using SharedKernel.Messaging.HealthChecks;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Observability;

public sealed class RabbitMqHealthCheckTests
{
    [Fact]
    public void ResolveConnectionString_UsesExplicitConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RabbitMQ"] = "amqp://guest:guest@broker:5672",
                    ["RabbitMQ:HostName"] = "legacy-host",
                }
            )
            .Build();

        string connectionString = RabbitMqConventionExtensions.ResolveConnectionString(
            configuration
        );

        connectionString.ShouldBe("amqp://guest:guest@broker:5672");
    }

    [Fact]
    public void ResolveConnectionString_WhenOnlyLegacyHostConfigured_UsesHostFallback()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["RabbitMQ:HostName"] = "rabbitmq:5672" }
            )
            .Build();

        string connectionString = RabbitMqConventionExtensions.ResolveConnectionString(
            configuration
        );

        connectionString.ShouldBe("amqp://rabbitmq:5672");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBrokerUnavailable_ReturnsUnhealthy()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RabbitMQ"] = "amqp://guest:guest@127.0.0.1:1",
                }
            )
            .Build();
        RabbitMqHealthCheck sut = new(configuration);

        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
