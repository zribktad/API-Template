using APITemplate.Api.GraphQL.Models;
using APITemplate.Application.Common.CQRS;
using HotChocolate.Authorization;

namespace APITemplate.Api.GraphQL.Queries;

/// <summary>
/// Hot Chocolate root query type that exposes product list and single-product lookups,
/// serving as the extension base for <see cref="CategoryQueries"/> and <see cref="ProductReviewQueries"/>.
/// </summary>
[Authorize]
public class ProductQueries
{
    /// <summary>
    /// Returns a paginated product list with search facets, mapping the GraphQL input to the
    /// application-layer filter before dispatching via the query handler.
    /// </summary>
    public async Task<ProductPageResult> GetProducts(
        ProductQueryInput? input,
        [Service] IQueryHandler<GetProductsQuery, ProductsResponse> handler,
        CancellationToken ct
    )
    {
        var filter = new ProductFilter(
            input?.Name,
            input?.Description,
            input?.MinPrice,
            input?.MaxPrice,
            input?.CreatedFrom,
            input?.CreatedTo,
            input?.SortBy,
            input?.SortDirection,
            input?.PageNumber ?? 1,
            input?.PageSize ?? PaginationFilter.DefaultPageSize,
            input?.Query,
            input?.CategoryIds
        );

        var page = await handler.HandleAsync(new GetProductsQuery(filter), ct);
        return new ProductPageResult(page.Page, page.Facets);
    }

    /// <summary>Returns a single product by ID, or <see langword="null"/> if not found.</summary>
    public async Task<ProductResponse?> GetProductById(
        Guid id,
        [Service] IQueryHandler<GetProductByIdQuery, ProductResponse?> handler,
        CancellationToken ct
    ) => await handler.HandleAsync(new GetProductByIdQuery(id), ct);
}
