using System.Diagnostics;
using SharedKernel.Infrastructure.Observability;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Observability;

public sealed class StartupTelemetryTests
{
    [Fact]
    public void StartRelationalMigration_WhenFailed_SetsFailureTags()
    {
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == ObservabilityConventions.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        using StartupTelemetry.Scope telemetry = StartupTelemetry.StartRelationalMigration();
        InvalidOperationException exception = new("boom");

        telemetry.Fail(exception);

        Activity.Current.ShouldNotBeNull();
        Activity
            .Current!.GetTagItem(TelemetryTagKeys.StartupStep)
            .ShouldBe(TelemetryStartupSteps.Migrate);
        Activity
            .Current!.GetTagItem(TelemetryTagKeys.StartupComponent)
            .ShouldBe(TelemetryStartupComponents.PostgreSql);
        Activity.Current!.GetTagItem(TelemetryTagKeys.StartupSuccess).ShouldBe(false);
    }
}
