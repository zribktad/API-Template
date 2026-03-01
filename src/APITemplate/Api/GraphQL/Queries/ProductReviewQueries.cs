using APITemplate.Application.DTOs;
using APITemplate.Application.Interfaces;

namespace APITemplate.Api.GraphQL.Queries;

[ExtendObjectType(typeof(ProductQueries))]
public class ProductReviewQueries
{
    public async Task<IReadOnlyList<ProductReviewResponse>> GetReviews(
        ProductReviewFilter filter,
        [Service] IProductReviewService reviewService,
        CancellationToken ct)
    {
        return await reviewService.GetAllAsync(filter, ct);
    }

    public async Task<ProductReviewResponse?> GetReviewById(
        Guid id,
        [Service] IProductReviewService reviewService,
        CancellationToken ct)
    {
        return await reviewService.GetByIdAsync(id, ct);
    }

    public async Task<IReadOnlyList<ProductReviewResponse>> GetReviewsByProductId(
        Guid productId,
        [Service] IProductReviewService reviewService,
        CancellationToken ct)
    {
        return await reviewService.GetByProductIdAsync(productId, ct);
    }
}
