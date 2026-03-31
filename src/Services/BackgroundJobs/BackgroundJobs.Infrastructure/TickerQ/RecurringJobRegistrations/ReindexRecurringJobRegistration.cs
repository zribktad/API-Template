using BackgroundJobs.Application.Common;
using BackgroundJobs.Application.Options;

namespace BackgroundJobs.Infrastructure.TickerQ.RecurringJobRegistrations;

/// <summary>
/// Provides the <see cref="RecurringBackgroundJobDefinition"/> for the reindex recurring job,
/// sourcing schedule and enablement from <see cref="BackgroundJobsOptions.Reindex"/>.
/// </summary>
public sealed class ReindexRecurringJobRegistration : IRecurringBackgroundJobRegistration
{
    /// <summary>Builds the reindex job definition from the supplied options.</summary>
    public RecurringBackgroundJobDefinition Build(BackgroundJobsOptions options) =>
        new(
            TickerQJobIds.Reindex,
            TickerQFunctionNames.Reindex,
            options.Reindex.Cron,
            options.Reindex.Enabled,
            "Rebuilds the PostgreSQL full-text search indexes."
        );
}
