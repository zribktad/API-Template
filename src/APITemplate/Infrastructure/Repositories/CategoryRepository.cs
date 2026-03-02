using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using APITemplate.Infrastructure.StoredProcedures;

namespace APITemplate.Infrastructure.Repositories;

public sealed class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    private readonly IStoredProcedureExecutor _spExecutor;

    public CategoryRepository(AppDbContext dbContext, IStoredProcedureExecutor spExecutor)
        : base(dbContext)
    {
        _spExecutor = spExecutor;
    }

    public Task<ProductCategoryStats?> GetStatsByIdAsync(Guid categoryId, CancellationToken ct = default)
    {
        return _spExecutor.QueryFirstAsync(new GetProductCategoryStatsProcedure(categoryId), ct);
    }
}
