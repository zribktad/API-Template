using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Options;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ.RecurringJobRegistrations;

public sealed class ExternalSyncRecurringJobRegistration : IRecurringBackgroundJobRegistration
{
    public RecurringBackgroundJobDefinition Build(BackgroundJobsOptions options) =>
        new(
            TickerQJobIds.ExternalSync,
            TickerQFunctionNames.ExternalSync,
            options.ExternalSync.Cron,
            options.ExternalSync.Enabled,
            "Runs periodic synchronization for configured external integrations."
        );
}
