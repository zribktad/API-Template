using Microsoft.EntityFrameworkCore;
using ProductCatalog.Domain.Entities;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace ProductCatalog.Infrastructure.Persistence.SoftDelete;

/// <summary>
/// Cascade rule that soft-deletes all <see cref="Product"/> entities belonging to a
/// <see cref="Category"/> when the category is soft-deleted.
/// </summary>
public sealed class CategorySoftDeleteCascadeRule : ISoftDeleteCascadeRule
{
    public bool CanHandle(IAuditableTenantEntity entity) => entity is Category;

    public async Task<IReadOnlyCollection<IAuditableTenantEntity>> GetDependentsAsync(
        DbContext dbContext,
        IAuditableTenantEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        Category category = (Category)entity;

        List<Product> products = await dbContext
            .Set<Product>()
            .IgnoreQueryFilters()
            .Where(p => p.CategoryId == category.Id && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        return products.Cast<IAuditableTenantEntity>().ToList();
    }
}
