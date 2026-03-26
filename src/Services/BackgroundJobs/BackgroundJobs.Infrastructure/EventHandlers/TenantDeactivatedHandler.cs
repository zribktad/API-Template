using BackgroundJobs.Domain.Entities;
using BackgroundJobs.Domain.Enums;
using BackgroundJobs.Infrastructure.Persistence;
using Contracts.IntegrationEvents.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackgroundJobs.Infrastructure.EventHandlers;

/// <summary>
/// Handles <see cref="TenantDeactivatedIntegrationEvent"/> by cancelling all pending/processing
/// job executions for the deactivated tenant using a single bulk update.
/// </summary>
public sealed class TenantDeactivatedHandler
{
    public static async Task HandleAsync(
        TenantDeactivatedIntegrationEvent message,
        BackgroundJobsDbContext dbContext,
        ILogger<TenantDeactivatedHandler> logger,
        CancellationToken ct
    )
    {
        int cancelled = await dbContext
            .JobExecutions.Where(j =>
                j.TenantId == message.TenantId
                && (j.Status == JobStatus.Pending || j.Status == JobStatus.Processing)
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(j => j.Status, JobStatus.Failed)
                        .SetProperty(j => j.ErrorMessage, "Tenant deactivated"),
                ct
            );

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
