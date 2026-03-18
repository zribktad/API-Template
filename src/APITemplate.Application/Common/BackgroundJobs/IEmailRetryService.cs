namespace APITemplate.Application.Common.BackgroundJobs;

public interface IEmailRetryService
{
    Task RetryFailedEmailsAsync(
        int maxRetryAttempts,
        int batchSize,
        CancellationToken ct = default
    );
    Task DeadLetterExpiredAsync(
        int deadLetterAfterHours,
        int batchSize,
        CancellationToken ct = default
    );
}
