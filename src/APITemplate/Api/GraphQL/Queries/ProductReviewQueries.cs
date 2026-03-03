namespace APITemplate.Api.GraphQL.Queries;

[ExtendObjectType(typeof(ProductQueries))]
public class ProductReviewQueries
{
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 20)]
    public async Task<IEnumerable<ProductReviewResponse>> GetReviews(
        [Service] IProductReviewQueryService queryService,
        CancellationToken ct)
        => await queryService.GetAllAsync(ct);

    public async Task<ProductReviewResponse?> GetReviewById(
        Guid id,
        [Service] IProductReviewQueryService queryService,
        CancellationToken ct)
        => await queryService.GetByIdAsync(id, ct);

    [UsePaging(MaxPageSize = 100, DefaultPageSize = 20)]
    public async Task<IEnumerable<ProductReviewResponse>> GetReviewsByProductId(
        Guid productId,
        [Service] IProductReviewQueryService queryService,
        CancellationToken ct)
        => await queryService.GetByProductIdAsync(productId, ct);
}
