using System.Diagnostics;

namespace SharedKernel.Infrastructure.Observability;

/// <summary>
/// Startup-phase telemetry helper for shared startup tasks.
/// </summary>
public static class StartupTelemetry
{
    public static Scope StartRelationalMigration() =>
        StartStep(
            TelemetryStartupSteps.Migrate,
            TelemetryStartupComponents.PostgreSql,
            TelemetryDatabaseSystems.PostgreSql
        );

    private static Scope StartStep(string step, string component, string? dbSystem = null)
    {
        Activity? activity = ObservabilityConventions.SharedActivitySource.StartActivity(
            TelemetryActivityNames.Startup(step),
            ActivityKind.Internal
        );
        activity?.SetTag(TelemetryTagKeys.StartupStep, step);
        activity?.SetTag(TelemetryTagKeys.StartupComponent, component);
        if (!string.IsNullOrWhiteSpace(dbSystem))
            activity?.SetTag(TelemetryTagKeys.DbSystem, dbSystem);

        return new Scope(activity);
    }

    public sealed class Scope(Activity? activity) : IDisposable
    {
        private readonly Activity? _activity = activity;

        public void Fail(Exception exception)
        {
            if (_activity is null)
                return;

            _activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            _activity.SetTag(TelemetryTagKeys.StartupSuccess, false);
            _activity.SetTag(TelemetryTagKeys.ExceptionType, exception.GetType().Name);
        }

        public void Dispose() => _activity?.Dispose();
    }
}
