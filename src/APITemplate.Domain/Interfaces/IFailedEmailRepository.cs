using APITemplate.Domain.Entities;

namespace APITemplate.Domain.Interfaces;

public interface IFailedEmailRepository
{
    Task AddAsync(FailedEmail failedEmail, CancellationToken ct = default);
    Task<List<FailedEmail>> ClaimRetryableBatchAsync(
        int maxRetryAttempts,
        int batchSize,
        string claimedBy,
        DateTime claimedAtUtc,
        DateTime claimedUntilUtc,
        CancellationToken ct = default
    );
    Task<List<FailedEmail>> ClaimExpiredBatchAsync(
        DateTime cutoff,
        int batchSize,
        string claimedBy,
        DateTime claimedAtUtc,
        DateTime claimedUntilUtc,
        CancellationToken ct = default
    );
    Task UpdateAsync(FailedEmail failedEmail, CancellationToken ct = default);
    Task DeleteAsync(FailedEmail failedEmail, CancellationToken ct = default);
}
