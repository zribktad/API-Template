using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product.Repositories;

/// <summary>
/// Domain-facing repository contract for products, extending the generic repository with product-specific filtered queries and facet aggregations.
/// </summary>
public interface IProductRepository : IRepository<ProductEntity>
{
    /// <summary>Returns a projected, filtered, sorted, and paged list of products.</summary>
    Task<IReadOnlyList<ProductResponse>> ListAsync(
        ProductFilter filter,
        CancellationToken ct = default
    );

    /// <summary>Returns the total number of products matching the given filter, used for pagination metadata.</summary>
    Task<int> CountAsync(ProductFilter filter, CancellationToken ct = default);

    /// <summary>Returns category facet counts for the current filter, ignoring any active category-ID constraints so all categories remain selectable.</summary>
    Task<IReadOnlyList<ProductCategoryFacetValue>> GetCategoryFacetsAsync(
        ProductFilter filter,
        CancellationToken ct = default
    );

    /// <summary>Returns price-bucket facet counts for the current filter, ignoring any active price-range constraints so all buckets remain selectable.</summary>
    Task<IReadOnlyList<ProductPriceFacetBucketResponse>> GetPriceFacetsAsync(
        ProductFilter filter,
        CancellationToken ct = default
    );
}
