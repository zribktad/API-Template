using APITemplate.Domain.Entities;

namespace APITemplate.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<ProductResponse>> ListAsync(ProductFilter filter, CancellationToken ct = default);
    Task<int> CountAsync(ProductFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCategoryFacetValue>> GetCategoryFacetsAsync(ProductFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ProductPriceFacetBucketResponse>> GetPriceFacetsAsync(ProductFilter filter, CancellationToken ct = default);
}
