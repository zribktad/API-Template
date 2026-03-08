using APITemplate.Application.Features.Product.Mappings;
using APITemplate.Application.Features.Product.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Product.Services;

public sealed class ProductQueryService : IProductQueryService
{
    private static readonly IReadOnlyList<ProductPriceFacetBucketResponse> DefaultPriceBuckets =
    [
        new("0 - 50", 0m, 50m, 0),
        new("50 - 100", 50m, 100m, 0),
        new("100 - 250", 100m, 250m, 0),
        new("250 - 500", 250m, 500m, 0),
        new("500+", 500m, null, 0)
    ];

    private readonly IProductRepository _repository;

    public ProductQueryService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductsResponse> GetPagedAsync(ProductFilter filter, CancellationToken ct = default)
    {
        var items = await _repository.ListAsync(new ProductSpecification(filter), ct);
        var totalCount = await _repository.CountAsync(new ProductCountSpecification(filter), ct);
        var categoryFacetSeeds = await _repository.ListAsync(new ProductCategoryFacetSeedSpecification(filter), ct);
        var priceFacetSeeds = await _repository.ListAsync(new ProductPriceFacetSeedSpecification(filter), ct);

        var categoryFacets = categoryFacetSeeds
            .GroupBy(seed => new { seed.CategoryId, seed.CategoryName })
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.CategoryName)
            .Select(group => new ProductCategoryFacetValue(
                group.Key.CategoryId,
                group.Key.CategoryName,
                group.Count()))
            .ToArray();

        var priceFacets = DefaultPriceBuckets
            .Select(bucket => bucket with
            {
                Count = priceFacetSeeds.Count(seed =>
                    seed.Price >= bucket.MinPrice &&
                    (bucket.MaxPrice is null || seed.Price < bucket.MaxPrice.Value))
            })
            .ToArray();

        return new ProductsResponse(
            new PagedResponse<ProductResponse>(items, totalCount, filter.PageNumber, filter.PageSize),
            new ProductSearchFacetsResponse(categoryFacets, priceFacets));
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.FirstOrDefaultAsync(new ProductByIdSpecification(id), ct);
    }
}
