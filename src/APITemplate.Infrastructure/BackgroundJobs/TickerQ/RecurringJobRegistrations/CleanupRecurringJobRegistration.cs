using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Options;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ.RecurringJobRegistrations;

public sealed class CleanupRecurringJobRegistration : IRecurringBackgroundJobRegistration
{
    public RecurringBackgroundJobDefinition Build(BackgroundJobsOptions options) =>
        new(
            TickerQJobIds.Cleanup,
            TickerQFunctionNames.Cleanup,
            options.Cleanup.Cron,
            options.Cleanup.Enabled,
            "Runs invitation, soft-delete, and orphaned ProductData cleanup."
        );
}
