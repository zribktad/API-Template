using APITemplate.Domain.Entities;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace APITemplate.Infrastructure.BackgroundJobs.Services;

public sealed class SoftDeleteCleanupStrategy<TEntity> : ISoftDeleteCleanupStrategy
    where TEntity : class, ISoftDeletable
{
    private readonly AppDbContext _dbContext;

    public SoftDeleteCleanupStrategy(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string EntityName => typeof(TEntity).Name;

    public async Task<int> CleanupAsync(
        DateTime cutoff,
        int batchSize,
        CancellationToken ct = default
    )
    {
        int totalDeleted = 0;
        int deleted;

        do
        {
            deleted = await _dbContext
                .Set<TEntity>()
                .IgnoreQueryFilters()
                .Where(e => e.IsDeleted && e.DeletedAtUtc < cutoff)
                .OrderBy(e => e.DeletedAtUtc)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);

            totalDeleted += deleted;
        } while (deleted == batchSize);

        return totalDeleted;
    }
}
