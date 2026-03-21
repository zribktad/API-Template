using APITemplate.Application.Common.CQRS;

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
        var page = await _repository.GetPagedAsync(request.Filter, ct);
        var categoryFacets = await _repository.GetCategoryFacetsAsync(request.Filter, ct);
        var priceFacets = await _repository.GetPriceFacetsAsync(request.Filter, ct);

        return new ProductsResponse(
            page,
            new ProductSearchFacetsResponse(categoryFacets, priceFacets)
        );
    }
}
