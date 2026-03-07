using APITemplate.Domain.Interfaces;

namespace APITemplate.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CommitAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);

    // For multi-step write flows only. Single writes should use repository + CommitAsync.
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            await action();
            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    // Same as above, but returns a value produced inside the transaction.
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await action();
            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
