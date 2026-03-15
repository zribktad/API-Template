using System.Diagnostics;

namespace APITemplate.Infrastructure.Observability;

public static class StartupTelemetry
{
    private static readonly ActivitySource ActivitySource = new(
        ObservabilityConventions.ActivitySourceName
    );

    public static Task RunRelationalMigrationAsync(Func<Task> action) =>
        RunStepAsync(
            TelemetryStartupSteps.Migrate,
            TelemetryStartupComponents.PostgreSql,
            action,
            TelemetryDatabaseSystems.PostgreSql
        );

    public static Task RunMongoMigrationAsync(Func<Task> action) =>
        RunStepAsync(
            TelemetryStartupSteps.Migrate,
            TelemetryStartupComponents.MongoDb,
            action,
            TelemetryDatabaseSystems.MongoDb
        );

    public static Task RunAuthBootstrapSeedAsync(Func<Task> action) =>
        RunStepAsync(
            TelemetryStartupSteps.SeedAuthBootstrap,
            TelemetryStartupComponents.AuthBootstrap,
            action
        );

    public static Task WaitForKeycloakReadinessAsync(Func<Task> action) =>
        RunStepAsync(
            TelemetryStartupSteps.WaitKeycloakReady,
            TelemetryStartupComponents.Keycloak,
            action
        );

    private static Task RunStepAsync(
        string step,
        string component,
        Func<Task> action,
        string? dbSystem = null
    ) => RunStepCoreAsync(step, component, action, dbSystem);

    private static async Task RunStepCoreAsync(
        string step,
        string component,
        Func<Task> action,
        string? dbSystem
    )
    {
        using var activity = StartActivity(step, component);
        if (!string.IsNullOrWhiteSpace(dbSystem))
            activity?.SetTag(TelemetryTagKeys.DbSystem, dbSystem);

        try
        {
            await action();
            activity?.SetTag(TelemetryTagKeys.StartupSuccess, true);
        }
        catch (Exception ex)
        {
            MarkFailure(activity, ex);
            throw;
        }
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

    private static void MarkFailure(Activity? activity, Exception exception)
    {
        if (activity is null)
            return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag(TelemetryTagKeys.StartupSuccess, false);
        activity.SetTag(TelemetryTagKeys.ExceptionType, exception.GetType().Name);
    }
}
