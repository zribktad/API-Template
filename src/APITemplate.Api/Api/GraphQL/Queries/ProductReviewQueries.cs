using APITemplate.Api.GraphQL.Models;
using HotChocolate.Authorization;
using Wolverine;

namespace APITemplate.Api.GraphQL.Queries;

/// <summary>
/// Hot Chocolate query type extension that adds product-review queries to the
/// <see cref="ProductQueries"/> root, supporting filtered list, single-item, and
/// per-product lookup operations.
/// </summary>
[Authorize]
[ExtendObjectType(typeof(ProductQueries))]
public class ProductReviewQueries
{
    /// <summary>
    /// Returns a paginated review list, mapping the GraphQL input to the application-layer
    /// filter before dispatching via the message bus.
    /// </summary>
    public async Task<ProductReviewPageResult> GetReviews(
        ProductReviewQueryInput? input,
        [Service] IMessageBus bus,
        CancellationToken ct
    )
    {
        var filter = new ProductReviewFilter(
            input?.ProductId,
            input?.UserId,
            input?.MinRating,
            input?.MaxRating,
            input?.CreatedFrom,
            input?.CreatedTo,
            input?.SortBy,
            input?.SortDirection,
            input?.PageNumber ?? 1,
            input?.PageSize ?? 20
        );

        var page = await bus.InvokeAsync<PagedResponse<ProductReviewResponse>>(
            new GetProductReviewsQuery(filter),
            ct
        );
        return new ProductReviewPageResult(page);
    }

    /// <summary>Returns a single review by ID, or <see langword="null"/> if not found.</summary>
    public async Task<ProductReviewResponse?> GetReviewById(
        Guid id,
        [Service] IMessageBus bus,
        CancellationToken ct
    ) => await bus.InvokeAsync<ProductReviewResponse?>(new GetProductReviewByIdQuery(id), ct);

    /// <summary>Returns a paginated list of reviews scoped to a specific product.</summary>
    public async Task<ProductReviewPageResult> GetReviewsByProductId(
        Guid productId,
        int pageNumber,
        int pageSize,
        [Service] IMessageBus bus,
        CancellationToken ct
    )
    {
        var filter = new ProductReviewFilter(
            ProductId: productId,
            PageNumber: pageNumber,
            PageSize: pageSize
        );
        var page = await bus.InvokeAsync<PagedResponse<ProductReviewResponse>>(
            new GetProductReviewsQuery(filter),
            ct
        );
        return new ProductReviewPageResult(page);
    }
}
