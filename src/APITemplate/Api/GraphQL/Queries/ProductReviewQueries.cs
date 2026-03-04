using APITemplate.Api.GraphQL.Models;
using HotChocolate.Authorization;

namespace APITemplate.Api.GraphQL.Queries;

[Authorize]
[ExtendObjectType(typeof(ProductQueries))]
public class ProductReviewQueries
{
    public async Task<ProductReviewPageResult> GetReviews(
        ProductReviewQueryInput? input,
        [Service] IProductReviewQueryService queryService,
        CancellationToken ct)
    {
        var filter = new ProductReviewFilter(
            input?.ProductId,
            input?.ReviewerName,
            input?.MinRating,
            input?.MaxRating,
            input?.CreatedFrom,
            input?.CreatedTo,
            input?.SortBy,
            input?.SortDirection,
            input?.PageNumber ?? 1,
            input?.PageSize ?? 20);

        var page = await queryService.GetPagedAsync(filter, ct);
        return new ProductReviewPageResult(page.Items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    public async Task<ProductReviewResponse?> GetReviewById(
        Guid id,
        [Service] IProductReviewQueryService queryService,
        CancellationToken ct)
        => await queryService.GetByIdAsync(id, ct);

    public async Task<ProductReviewPageResult> GetReviewsByProductId(
        Guid productId,
        int pageNumber,
        int pageSize,
        [Service] IProductReviewQueryService queryService,
        CancellationToken ct)
    {
        var page = await queryService.GetPagedAsync(
            new ProductReviewFilter(ProductId: productId, PageNumber: pageNumber, PageSize: pageSize),
            ct);

        return new ProductReviewPageResult(page.Items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
