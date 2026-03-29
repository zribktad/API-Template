namespace BackgroundJobs.Application.Common;

/// <summary>
/// Application-layer contract for the cross-service email retry operation.
/// Implementations live in the Infrastructure layer and are invoked by recurring background jobs.
/// </summary>
public interface IEmailRetryService
{
    /// <summary>
    /// Claims and retries failed emails from the Notifications database, updating status
    /// on success or failure and dead-lettering emails that exceed the configured threshold.
    /// </summary>
    Task RetryFailedEmailsAsync(CancellationToken cancellationToken = default);
}
