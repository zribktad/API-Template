using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedKernel.Infrastructure.Observability;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Observability;

public sealed class HealthCheckMetricsPublisherTests
{
    [Fact]
    public async Task PublishAsync_StoresLatestHealthStatuses()
    {
        HealthCheckMetricsPublisher publisher = new();
        HealthReport report = new(
            new Dictionary<string, HealthReportEntry>
            {
                ["postgresql"] = new(HealthStatus.Healthy, "ok", TimeSpan.Zero, null, null),
                ["redis"] = new(HealthStatus.Unhealthy, "down", TimeSpan.Zero, null, null),
            },
            TimeSpan.Zero
        );

        await publisher.PublishAsync(report, TestContext.Current.CancellationToken);

        IReadOnlyDictionary<string, int> statuses = HealthCheckMetricsPublisher.SnapshotStatuses();
        statuses["postgresql"].ShouldBe(1);
        statuses["redis"].ShouldBe(0);
    }
}
