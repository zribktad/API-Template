using ErrorOr;
using Reviews.Application.Common.Responses;
using Reviews.Application.Features.GetReviewsByProductIds;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Features.GetReviewsByProductId;

/// <summary>Returns all reviews for a specific product, ordered by creation date descending.</summary>
public sealed record GetProductReviewsByProductIdQuery(Guid ProductId);

/// <summary>Handles <see cref="GetProductReviewsByProductIdQuery"/>.</summary>
public sealed class GetProductReviewsByProductIdQueryHandler
{
    public static async Task<ErrorOr<IReadOnlyList<ProductReviewResponse>>> HandleAsync(
        GetProductReviewsByProductIdQuery request,
        IProductReviewRepository reviewRepository,
        CancellationToken ct
    )
    {
        return await reviewRepository.ListAsync(
            new GetReviewsByProductIdsSpec([request.ProductId]),
            ct
        );
    }
}
