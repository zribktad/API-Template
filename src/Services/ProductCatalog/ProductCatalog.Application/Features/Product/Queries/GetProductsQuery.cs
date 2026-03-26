using ErrorOr;
using ProductCatalog.Application.Features.Product.DTOs;
using ProductCatalog.Application.Features.Product.Repositories;

namespace ProductCatalog.Application.Features.Product.Queries;

/// <summary>Retrieves a filtered, sorted, and paged list of products together with search facets.</summary>
public sealed record GetProductsQuery(ProductFilter Filter);

/// <summary>Handles <see cref="GetProductsQuery"/> by fetching items, count, and facets from the repository.</summary>
public sealed class GetProductsQueryHandler
{
    public static async Task<ErrorOr<ProductsResponse>> HandleAsync(
        GetProductsQuery request,
        IProductRepository repository,
        CancellationToken ct
    )
    {
        SharedKernel.Domain.Common.PagedResponse<ProductResponse> page =
            await repository.GetPagedAsync(request.Filter, ct);
        IReadOnlyList<ProductCategoryFacetValue> categoryFacets =
            await repository.GetCategoryFacetsAsync(request.Filter, ct);
        IReadOnlyList<ProductPriceFacetBucketResponse> priceFacets =
            await repository.GetPriceFacetsAsync(request.Filter, ct);

        return new ProductsResponse(
            page,
            new ProductSearchFacetsResponse(categoryFacets, priceFacets)
        );
    }
}
