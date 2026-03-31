using Microsoft.EntityFrameworkCore;
using ProductCatalog.Domain.Interfaces;
using ProductCatalog.Infrastructure.Persistence;

namespace ProductCatalog.Infrastructure.StoredProcedures;

/// <summary>
/// EF Core implementation of <see cref="IStoredProcedureExecutor"/>.
/// </summary>
public sealed class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly ProductCatalogDbContext _dbContext;

    public StoredProcedureExecutor(ProductCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TResult?> QueryFirstAsync<TResult>(
        IStoredProcedure<TResult> procedure,
        CancellationToken ct = default
    )
        where TResult : class =>
        _dbContext.Set<TResult>().FromSql(procedure.ToSql()).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TResult>> QueryManyAsync<TResult>(
        IStoredProcedure<TResult> procedure,
        CancellationToken ct = default
    )
        where TResult : class =>
        await _dbContext.Set<TResult>().FromSql(procedure.ToSql()).ToListAsync(ct);

    public Task<int> ExecuteAsync(FormattableString sql, CancellationToken ct = default) =>
        _dbContext.Database.ExecuteSqlAsync(sql, ct);
}
