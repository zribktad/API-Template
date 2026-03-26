using Microsoft.EntityFrameworkCore;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;
using ProductCatalog.Infrastructure.Persistence;
using SharedKernel.Application.Context;

namespace ProductCatalog.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="ProductDataLink"/> join entities, providing queries
/// that selectively bypass global filters when deleted links must be included.
/// </summary>
public sealed class ProductDataLinkRepository : IProductDataLinkRepository
{
    private readonly ProductCatalogDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;

    public ProductDataLinkRepository(
        ProductCatalogDbContext dbContext,
        ITenantProvider tenantProvider
    )
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<ProductDataLink>> ListByProductIdAsync(
        Guid productId,
        bool includeDeleted = false,
        CancellationToken ct = default
    )
    {
        IQueryable<ProductDataLink> query = includeDeleted
            ? _dbContext
                .ProductDataLinks.IgnoreQueryFilters()
                .Where(link =>
                    link.TenantId == _tenantProvider.TenantId && link.ProductId == productId
                )
            : _dbContext.ProductDataLinks.Where(link => link.ProductId == productId);

        return await query.ToListAsync(ct);
    }

    public async Task<
        IReadOnlyDictionary<Guid, IReadOnlyList<ProductDataLink>>
    > ListByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        bool includeDeleted = false,
        CancellationToken ct = default
    )
    {
        if (productIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<ProductDataLink>>();

        IQueryable<ProductDataLink> query = includeDeleted
            ? _dbContext
                .ProductDataLinks.IgnoreQueryFilters()
                .Where(link =>
                    link.TenantId == _tenantProvider.TenantId && productIds.Contains(link.ProductId)
                )
            : _dbContext.ProductDataLinks.Where(link => productIds.Contains(link.ProductId));

        List<ProductDataLink> links = await query.ToListAsync(ct);
        return links
            .GroupBy(link => link.ProductId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ProductDataLink>)group.ToList()
            );
    }

    public Task<bool> HasActiveLinksForProductDataAsync(
        Guid productDataId,
        CancellationToken ct = default
    ) => _dbContext.ProductDataLinks.AnyAsync(link => link.ProductDataId == productDataId, ct);

    public async Task SoftDeleteActiveLinksForProductDataAsync(
        Guid productDataId,
        CancellationToken ct = default
    )
    {
        List<ProductDataLink> links = await _dbContext
            .ProductDataLinks.Where(link => link.ProductDataId == productDataId)
            .ToListAsync(ct);

        if (links.Count == 0)
            return;

        _dbContext.ProductDataLinks.RemoveRange(links);
    }
}
