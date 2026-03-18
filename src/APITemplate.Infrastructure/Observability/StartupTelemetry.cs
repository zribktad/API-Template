using System.Diagnostics;

namespace APITemplate.Infrastructure.Observability;

public static class StartupTelemetry
{
    private static readonly ActivitySource ActivitySource = new(
        ObservabilityConventions.ActivitySourceName
    );

    public static Scope StartRelationalMigration() =>
        StartStep(
            TelemetryStartupSteps.Migrate,
            TelemetryStartupComponents.PostgreSql,
            TelemetryDatabaseSystems.PostgreSql
        );

    public static Scope StartMongoMigration() =>
        StartStep(
            TelemetryStartupSteps.Migrate,
            TelemetryStartupComponents.MongoDb,
            TelemetryDatabaseSystems.MongoDb
        );

    public static Scope StartAuthBootstrapSeed() =>
        StartStep(
            TelemetryStartupSteps.SeedAuthBootstrap,
            TelemetryStartupComponents.AuthBootstrap
        );

    public static Scope StartKeycloakReadinessCheck() =>
        StartStep(TelemetryStartupSteps.WaitKeycloakReady, TelemetryStartupComponents.Keycloak);

    private static Scope StartStep(string step, string component, string? dbSystem = null)
    {
        var activity = StartActivity(step, component);
        if (!string.IsNullOrWhiteSpace(dbSystem))
            activity?.SetTag(TelemetryTagKeys.DbSystem, dbSystem);

        return new Scope(activity);
    }

    private static Activity? StartActivity(string step, string component)
    {
        var activity = ActivitySource.StartActivity(
            TelemetryActivityNames.Startup(step),
            ActivityKind.Internal
        );
        activity?.SetTag(TelemetryTagKeys.StartupStep, step);
        activity?.SetTag(TelemetryTagKeys.StartupComponent, component);
        return activity;
    }

    public sealed class Scope(Activity? activity) : IDisposable
    {
        private readonly Activity? _activity = activity;

        public void Fail(Exception exception)
        {
            if (_activity is not null)
            {
                _activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                _activity.SetTag(TelemetryTagKeys.StartupSuccess, false);
                _activity.SetTag(TelemetryTagKeys.ExceptionType, exception.GetType().Name);
            }
        }

        public void Dispose() => _activity?.Dispose();
    }
}
