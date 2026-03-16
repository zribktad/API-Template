using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Options;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ.RecurringJobRegistrations;

public sealed class ReindexRecurringJobRegistration : IRecurringBackgroundJobRegistration
{
    public RecurringBackgroundJobDefinition Build(BackgroundJobsOptions options) =>
        new(
            TickerQJobIds.Reindex,
            TickerQFunctionNames.Reindex,
            options.Reindex.Cron,
            options.Reindex.Enabled,
            "Rebuilds the PostgreSQL full-text search indexes."
        );
}
