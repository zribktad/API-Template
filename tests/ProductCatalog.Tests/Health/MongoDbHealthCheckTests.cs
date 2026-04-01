using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using ProductCatalog.Api.Health;
using ProductCatalog.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace ProductCatalog.Tests.Health;

public sealed class MongoDbHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenPingSucceeds_ReturnsHealthy()
    {
        Mock<IMongoDbHealthProbe> probe = new();
        MongoDbHealthCheck sut = new(probe.Object);

        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingFails_ReturnsUnhealthy()
    {
        Mock<IMongoDbHealthProbe> probe = new();
        probe
            .Setup(x => x.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        MongoDbHealthCheck sut = new(probe.Object);

        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
