using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Entities;
using Notifications.Domain.Interfaces;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="FailedEmail"/> that provides CRUD operations
/// against the <see cref="NotificationsDbContext"/>.
/// </summary>
public sealed class FailedEmailRepository : IFailedEmailRepository
{
    private readonly NotificationsDbContext _dbContext;

    public FailedEmailRepository(NotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Stages the failed email for insertion without flushing to the database.</summary>
    public Task AddAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Add(failedEmail);
        return Task.CompletedTask;
    }

    /// <summary>Returns all failed email records ordered by creation date descending.</summary>
    public async Task<IReadOnlyList<FailedEmail>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.FailedEmails.OrderByDescending(e => e.CreatedAtUtc).ToListAsync(ct);
    }

    /// <summary>Returns a single failed email by its identifier, or null if not found.</summary>
    public async Task<FailedEmail?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.FailedEmails.FindAsync([id], ct);
    }

    /// <summary>Claims a batch of retryable failed emails for processing.</summary>
    public async Task<List<FailedEmail>> ClaimRetryableBatchAsync(
        int maxRetryAttempts,
        int batchSize,
        string claimedBy,
        DateTime claimedAtUtc,
        DateTime claimedUntilUtc,
        CancellationToken ct = default
    )
    {
        List<FailedEmail> emails = await _dbContext
            .FailedEmails.Where(e =>
                !e.IsDeadLettered
                && e.RetryCount < maxRetryAttempts
                && (e.ClaimedUntilUtc == null || e.ClaimedUntilUtc < claimedAtUtc)
            )
            .OrderBy(e => e.LastAttemptAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        foreach (FailedEmail email in emails)
        {
            email.ClaimedBy = claimedBy;
            email.ClaimedAtUtc = claimedAtUtc;
            email.ClaimedUntilUtc = claimedUntilUtc;
        }

        await _dbContext.SaveChangesAsync(ct);
        return emails;
    }

    /// <summary>Claims a batch of expired (stale claim) failed emails for reprocessing.</summary>
    public async Task<List<FailedEmail>> ClaimExpiredBatchAsync(
        DateTime cutoff,
        int batchSize,
        string claimedBy,
        DateTime claimedAtUtc,
        DateTime claimedUntilUtc,
        CancellationToken ct = default
    )
    {
        List<FailedEmail> emails = await _dbContext
            .FailedEmails.Where(e =>
                !e.IsDeadLettered && e.ClaimedUntilUtc != null && e.ClaimedUntilUtc < cutoff
            )
            .OrderBy(e => e.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        foreach (FailedEmail email in emails)
        {
            email.ClaimedBy = claimedBy;
            email.ClaimedAtUtc = claimedAtUtc;
            email.ClaimedUntilUtc = claimedUntilUtc;
        }

        await _dbContext.SaveChangesAsync(ct);
        return emails;
    }

    /// <summary>Stages an update for the failed email without flushing to the database.</summary>
    public Task UpdateAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Update(failedEmail);
        return Task.CompletedTask;
    }

    /// <summary>Stages a hard delete (physical removal) for the failed email without flushing to the database.</summary>
    public Task DeleteAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Remove(failedEmail);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
