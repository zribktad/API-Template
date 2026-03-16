using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace APITemplate.Infrastructure.Repositories;

public sealed class FailedEmailRepository : IFailedEmailRepository
{
    private readonly AppDbContext _dbContext;

    public FailedEmailRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Add(failedEmail);
        return Task.CompletedTask;
    }

    public async Task<List<FailedEmail>> GetRetryableAsync(
        int maxRetryAttempts,
        int batchSize,
        CancellationToken ct = default
    )
    {
        return await _dbContext
            .FailedEmails.Where(e => !e.IsDeadLettered && e.RetryCount < maxRetryAttempts)
            .OrderBy(e => e.LastAttemptAtUtc ?? e.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<List<FailedEmail>> GetExpiredAsync(
        DateTime cutoff,
        int batchSize,
        CancellationToken ct = default
    )
    {
        return await _dbContext
            .FailedEmails.Where(e => !e.IsDeadLettered && e.CreatedAtUtc < cutoff)
            .OrderBy(e => e.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public Task UpdateAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Update(failedEmail);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Remove(failedEmail);
        return Task.CompletedTask;
    }
}
