using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Options;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ.RecurringJobRegistrations;

public sealed class EmailRetryRecurringJobRegistration : IRecurringBackgroundJobRegistration
{
    public RecurringBackgroundJobDefinition Build(BackgroundJobsOptions options) =>
        new(
            TickerQJobIds.EmailRetry,
            TickerQFunctionNames.EmailRetry,
            options.EmailRetry.Cron,
            options.EmailRetry.Enabled,
            "Retries failed emails and dead-letters expired retry records."
        );
}
