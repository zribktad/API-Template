using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace BackgroundJobs.Application.Options;

/// <summary>
/// Validates <see cref="BackgroundJobsOptions"/> beyond what data annotations support,
/// including cross-field constraints and cron expression format checks.
/// </summary>
public sealed partial class BackgroundJobsOptionsValidator : IValidateOptions<BackgroundJobsOptions>
{
    public ValidateOptionsResult Validate(string? name, BackgroundJobsOptions options)
    {
        List<string> failures = [];

        ValidateEmailRetryOptions(options.EmailRetry, failures);
        ValidateCronExpressions(options, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateEmailRetryOptions(
        EmailRetryJobOptions emailRetry,
        List<string> failures
    )
    {
        if (emailRetry.MaxRetryAttempts is < 1 or > 20)
        {
            failures.Add(
                $"{nameof(BackgroundJobsOptions.EmailRetry)}.{nameof(EmailRetryJobOptions.MaxRetryAttempts)} must be between 1 and 20, but was {emailRetry.MaxRetryAttempts}."
            );
        }

        if (emailRetry.BatchSize is < 1 or > 500)
        {
            failures.Add(
                $"{nameof(BackgroundJobsOptions.EmailRetry)}.{nameof(EmailRetryJobOptions.BatchSize)} must be between 1 and 500, but was {emailRetry.BatchSize}."
            );
        }

        if (emailRetry.DeadLetterAfterHours < 1)
        {
            failures.Add(
                $"{nameof(BackgroundJobsOptions.EmailRetry)}.{nameof(EmailRetryJobOptions.DeadLetterAfterHours)} must be at least 1, but was {emailRetry.DeadLetterAfterHours}."
            );
        }
    }

    private static void ValidateCronExpressions(
        BackgroundJobsOptions options,
        List<string> failures
    )
    {
        ValidateCronExpression(
            $"{nameof(BackgroundJobsOptions.Cleanup)}.{nameof(CleanupJobOptions.Cron)}",
            options.Cleanup.Cron,
            failures
        );

        ValidateCronExpression(
            $"{nameof(BackgroundJobsOptions.Reindex)}.{nameof(ReindexJobOptions.Cron)}",
            options.Reindex.Cron,
            failures
        );

        ValidateCronExpression(
            $"{nameof(BackgroundJobsOptions.EmailRetry)}.{nameof(EmailRetryJobOptions.Cron)}",
            options.EmailRetry.Cron,
            failures
        );
    }

    private static void ValidateCronExpression(
        string propertyPath,
        string cron,
        List<string> failures
    )
    {
        if (string.IsNullOrWhiteSpace(cron) || !CronFormatRegex().IsMatch(cron))
        {
            failures.Add(
                $"{propertyPath} must be a valid 5-part cron expression, but was '{cron}'."
            );
        }
    }

    /// <summary>
    /// Basic 5-part cron format validation: minute hour day-of-month month day-of-week.
    /// Each field allows digits, wildcards, ranges, steps, and lists.
    /// </summary>
    [GeneratedRegex(@"^(\S+\s+){4}\S+$")]
    private static partial Regex CronFormatRegex();
}
