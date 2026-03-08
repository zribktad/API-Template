using APITemplate.Application.Features.Product.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Product.Services;

public sealed class ProductQueryService : IProductQueryService
{
    private readonly IProductRepository _productRepository;

    public ProductQueryService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductsResponse> GetPagedAsync(ProductFilter filter, CancellationToken ct = default)
    {
        var itemsTask = _productRepository.ListAsync(filter, ct);
        var totalCountTask = _productRepository.CountAsync(filter, ct);
        var categoryFacetsTask = _productRepository.GetCategoryFacetsAsync(filter, ct);
        var priceFacetsTask = _productRepository.GetPriceFacetsAsync(filter, ct);

        await Task.WhenAll(itemsTask, totalCountTask, categoryFacetsTask, priceFacetsTask);

        return new ProductsResponse(
            new PagedResponse<ProductResponse>(itemsTask.Result, totalCountTask.Result, filter.PageNumber, filter.PageSize),
            new ProductSearchFacetsResponse(categoryFacetsTask.Result, priceFacetsTask.Result));
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _productRepository.FirstOrDefaultAsync(new ProductByIdSpecification(id), ct);
    }
}
