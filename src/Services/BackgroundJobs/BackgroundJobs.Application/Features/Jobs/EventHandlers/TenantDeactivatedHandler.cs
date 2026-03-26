using BackgroundJobs.Domain.Entities;
using BackgroundJobs.Domain.Enums;
using BackgroundJobs.Domain.Interfaces;
using Contracts.IntegrationEvents.Identity;
using Microsoft.Extensions.Logging;

namespace BackgroundJobs.Application.Features.Jobs.EventHandlers;

/// <summary>
/// Handles <see cref="TenantDeactivatedIntegrationEvent"/> by cancelling all pending/processing
/// job executions for the deactivated tenant.
/// </summary>
public sealed class TenantDeactivatedHandler
{
    public static async Task HandleAsync(
        TenantDeactivatedIntegrationEvent message,
        IJobExecutionRepository repository,
        TimeProvider timeProvider,
        ILogger<TenantDeactivatedHandler> logger,
        CancellationToken ct
    )
    {
        IEnumerable<JobExecution> pendingJobs = await repository.ListAsync(ct);

        int cancelled = 0;
        foreach (JobExecution job in pendingJobs)
        {
            if (job.TenantId != message.TenantId)
                continue;

            if (job.Status is not (JobStatus.Pending or JobStatus.Processing))
                continue;

            job.MarkFailed("Tenant deactivated", timeProvider);
            cancelled++;
        }

        if (cancelled > 0)
        {
            logger.LogInformation(
                "Cancelled {Count} pending/processing jobs for deactivated tenant {TenantId}.",
                cancelled,
                message.TenantId
            );
        }
    }
}
