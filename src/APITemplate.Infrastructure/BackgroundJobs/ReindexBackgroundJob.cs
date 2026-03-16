using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.BackgroundJobs;

public sealed class ReindexBackgroundJob : PeriodicBackgroundJob
{
    public ReindexBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ReindexBackgroundJob> logger,
        IOptions<BackgroundJobsOptions> options
    )
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromMinutes(options.Value.Reindex.IntervalMinutes),
            nameof(ReindexBackgroundJob)
        ) { }

    protected override async Task ExecuteJobAsync(
        IServiceProvider serviceProvider,
        CancellationToken ct
    )
    {
        var service = serviceProvider.GetRequiredService<IReindexService>();
        await service.ReindexFullTextSearchAsync(ct);
    }
}
