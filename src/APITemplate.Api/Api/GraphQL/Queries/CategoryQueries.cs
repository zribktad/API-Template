using APITemplate.Api.GraphQL.Models;
using APITemplate.Application.Common.CQRS;
using HotChocolate.Authorization;

namespace APITemplate.Api.GraphQL.Queries;

/// <summary>
/// Hot Chocolate query type extension that adds category queries to the <see cref="ProductQueries"/>
/// root, providing paginated list and single-item lookup operations.
/// </summary>
[Authorize]
[ExtendObjectType(typeof(ProductQueries))]
public sealed class CategoryQueries
{
    /// <summary>
    /// Returns a paginated category list, mapping the GraphQL input to the application-layer
    /// filter before dispatching via the query handler.
    /// </summary>
    public async Task<CategoryPageResult> GetCategories(
        CategoryQueryInput? input,
        [Service] IQueryHandler<GetCategoriesQuery, PagedResponse<CategoryResponse>> handler,
        CancellationToken ct
    )
    {
        var filter = new CategoryFilter(
            input?.Query,
            input?.SortBy,
            input?.SortDirection,
            input?.PageNumber ?? 1,
            input?.PageSize ?? PaginationFilter.DefaultPageSize
        );

        var page = await handler.HandleAsync(new GetCategoriesQuery(filter), ct);
        return new CategoryPageResult(page);
    }

    /// <summary>Returns a single category by ID, or <see langword="null"/> if not found.</summary>
    public async Task<CategoryResponse?> GetCategoryById(
        Guid id,
        [Service] IQueryHandler<GetCategoryByIdQuery, CategoryResponse?> handler,
        CancellationToken ct
    ) => await handler.HandleAsync(new GetCategoryByIdQuery(id), ct);
}
