using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Product.Repositories;

namespace APITemplate.Application.Features.Product;

/// <summary>Retrieves a filtered, sorted, and paged list of products together with search facets.</summary>
public sealed record GetProductsQuery(ProductFilter Filter) : IQuery<ProductsResponse>;

/// <summary>Handles <see cref="GetProductsQuery"/> by fetching items, count, and facets from the repository.</summary>
public sealed class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, ProductsResponse>
{
    private readonly IProductRepository _repository;

    public GetProductsQueryHandler(IProductRepository repository) => _repository = repository;

    public async Task<ProductsResponse> HandleAsync(GetProductsQuery request, CancellationToken ct)
    {
        var items = await _repository.ListAsync(request.Filter, ct);
        var totalCount = await _repository.CountAsync(request.Filter, ct);
        var categoryFacets = await _repository.GetCategoryFacetsAsync(request.Filter, ct);
        var priceFacets = await _repository.GetPriceFacetsAsync(request.Filter, ct);

        return new ProductsResponse(
            new PagedResponse<ProductResponse>(
                items,
                totalCount,
                request.Filter.PageNumber,
                request.Filter.PageSize
            ),
            new ProductSearchFacetsResponse(categoryFacets, priceFacets)
        );
    }
}
