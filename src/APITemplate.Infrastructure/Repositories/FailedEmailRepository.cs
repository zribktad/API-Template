using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using APITemplate.Infrastructure.StoredProcedures;
using Microsoft.EntityFrameworkCore;

namespace APITemplate.Infrastructure.Repositories;

public sealed class FailedEmailRepository : IFailedEmailRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IStoredProcedureExecutor _executor;

    public FailedEmailRepository(AppDbContext dbContext, IStoredProcedureExecutor executor)
    {
        _dbContext = dbContext;
        _executor = executor;
    }

    public Task AddAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Add(failedEmail);
        return Task.CompletedTask;
    }

    public async Task<List<FailedEmail>> ClaimRetryableBatchAsync(
        int maxRetryAttempts,
        int batchSize,
        string claimedBy,
        DateTime claimedAtUtc,
        DateTime claimedUntilUtc,
        CancellationToken ct = default
    )
    {
        var procedure = new ClaimRetryableFailedEmailsProcedure(
            maxRetryAttempts,
            batchSize,
            claimedBy,
            claimedAtUtc,
            claimedUntilUtc
        );
        var result = await _executor.QueryManyAsync(procedure, ct);
        return result.ToList();
    }

    public async Task<List<FailedEmail>> ClaimExpiredBatchAsync(
        DateTime cutoff,
        int batchSize,
        string claimedBy,
        DateTime claimedAtUtc,
        DateTime claimedUntilUtc,
        CancellationToken ct = default
    )
    {
        var procedure = new ClaimExpiredFailedEmailsProcedure(
            cutoff,
            batchSize,
            claimedBy,
            claimedAtUtc,
            claimedUntilUtc
        );
        var result = await _executor.QueryManyAsync(procedure, ct);
        return result.ToList();
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
