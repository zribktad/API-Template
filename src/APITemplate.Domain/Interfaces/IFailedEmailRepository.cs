using APITemplate.Domain.Entities;

namespace APITemplate.Domain.Interfaces;

public interface IFailedEmailRepository
{
    Task AddAsync(FailedEmail failedEmail, CancellationToken ct = default);
    Task<List<FailedEmail>> GetRetryableAsync(
        int maxRetryAttempts,
        int batchSize,
        CancellationToken ct = default
    );
    Task<List<FailedEmail>> GetExpiredAsync(
        DateTime cutoff,
        int batchSize,
        CancellationToken ct = default
    );
    Task UpdateAsync(FailedEmail failedEmail, CancellationToken ct = default);
    Task DeleteAsync(FailedEmail failedEmail, CancellationToken ct = default);
}
