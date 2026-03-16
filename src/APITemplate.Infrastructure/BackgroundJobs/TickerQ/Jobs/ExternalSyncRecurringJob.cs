using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Infrastructure.BackgroundJobs.TickerQ.Coordination;
using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Base;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ.Jobs;

public sealed class ExternalSyncRecurringJob
{
    private readonly IExternalIntegrationSyncService _syncService;
    private readonly IDistributedJobCoordinator _coordinator;
    private readonly ILogger<ExternalSyncRecurringJob> _logger;

    public ExternalSyncRecurringJob(
        IExternalIntegrationSyncService syncService,
        IDistributedJobCoordinator coordinator,
        ILogger<ExternalSyncRecurringJob> logger
    )
    {
        _syncService = syncService;
        _coordinator = coordinator;
        _logger = logger;
    }

    [TickerFunction(TickerQFunctionNames.ExternalSync)]
    public Task ExecuteAsync(TickerFunctionContext context, CancellationToken ct) =>
        _coordinator.ExecuteIfLeaderAsync(
            TickerQFunctionNames.ExternalSync,
            async token =>
            {
                _logger.LogInformation(
                    "Executing external integration sync recurring job for ticker {TickerId}.",
                    context.Id
                );
                await _syncService.SynchronizeAsync(token);
            },
            ct
        );
}
