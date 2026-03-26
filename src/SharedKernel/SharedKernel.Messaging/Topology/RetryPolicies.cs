using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.ErrorHandling;

namespace SharedKernel.Messaging.Topology;

/// <summary>
/// Shared retry and error handling policies for Wolverine message processing,
/// applied consistently across all microservices.
/// </summary>
public static class RetryPolicies
{
    /// <summary>
    /// Applies shared retry policies for common transient and concurrency failures.
    /// </summary>
    public static WolverineOptions ApplySharedRetryPolicies(this WolverineOptions opts)
    {
        opts.OnException<DbUpdateException>()
            .RetryWithCooldown(
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(250)
            )
            .Then.MoveToErrorQueue();

        opts.OnException<DbUpdateConcurrencyException>()
            .RetryWithCooldown(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500))
            .Then.MoveToErrorQueue();

        opts.OnException<TimeoutException>()
            .ScheduleRetry(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(2)
            )
            .Then.MoveToErrorQueue();

        return opts;
    }
}
