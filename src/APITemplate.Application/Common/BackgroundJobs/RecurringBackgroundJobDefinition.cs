namespace APITemplate.Application.Common.BackgroundJobs;

public sealed record RecurringBackgroundJobDefinition(
    Guid Id,
    string FunctionName,
    string CronExpression,
    bool Enabled,
    string Description,
    int Retries = 0,
    int[]? RetryIntervals = null
);
