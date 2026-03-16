using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.BackgroundJobs;

public sealed class CleanupBackgroundJob : PeriodicBackgroundJob
{
    private readonly CleanupJobOptions _options;

    public CleanupBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ILogger<CleanupBackgroundJob> logger,
        IOptions<BackgroundJobsOptions> options
    )
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromMinutes(options.Value.Cleanup.IntervalMinutes),
            nameof(CleanupBackgroundJob)
        )
    {
        _options = options.Value.Cleanup;
    }

    protected override async Task ExecuteJobAsync(
        IServiceProvider serviceProvider,
        CancellationToken ct
    )
    {
        var service = serviceProvider.GetRequiredService<ICleanupService>();

        await service.CleanupExpiredInvitationsAsync(
            _options.ExpiredInvitationRetentionHours,
            _options.BatchSize,
            ct
        );

        await service.CleanupSoftDeletedRecordsAsync(
            _options.SoftDeleteRetentionDays,
            _options.BatchSize,
            ct
        );

        await service.CleanupOrphanedProductDataAsync(
            _options.OrphanedProductDataRetentionDays,
            _options.BatchSize,
            ct
        );
    }
}
