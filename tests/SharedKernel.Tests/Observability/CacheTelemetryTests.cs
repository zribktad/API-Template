using SharedKernel.Infrastructure.Observability;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Observability;

public sealed class CacheTelemetryTests
{
    [Fact]
    public void StartOutputCacheInvalidationActivity_AddsCacheTag()
    {
        using System.Diagnostics.Activity? activity =
            CacheTelemetry.StartOutputCacheInvalidationActivity("Products");

        activity.ShouldNotBeNull();
        activity.GetTagItem(TelemetryTagKeys.CacheTag).ShouldBe("Products");
    }
}
