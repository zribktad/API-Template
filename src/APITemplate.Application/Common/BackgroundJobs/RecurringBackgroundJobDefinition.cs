namespace APITemplate.Application.Common.BackgroundJobs;

/// <summary>
/// Immutable descriptor for a recurring background job passed from the Application layer to the
/// Infrastructure scheduler (e.g. Hangfire). Each <see cref="IRecurringBackgroundJobRegistration"/>
/// produces one instance of this record.
/// </summary>
public sealed record RecurringBackgroundJobDefinition(
    /// <summary>Stable identifier for the job, used to upsert the schedule in the scheduler.</summary>
    Guid Id,
    /// <summary>The scheduler entry-point function name (e.g. Hangfire job method name).</summary>
    string FunctionName,
    /// <summary>Cron expression that controls the execution frequency.</summary>
    string CronExpression,
    /// <summary>When <c>false</c> the scheduler should skip or remove this job without error.</summary>
    bool Enabled,
    /// <summary>Human-readable description shown in the scheduler dashboard.</summary>
    string Description,
    /// <summary>Number of automatic retry attempts on failure.</summary>
    int Retries = 0,
    /// <summary>Optional delay intervals (in seconds) between consecutive retry attempts.</summary>
    int[]? RetryIntervals = null
);
