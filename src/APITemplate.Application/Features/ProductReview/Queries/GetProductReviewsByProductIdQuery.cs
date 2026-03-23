using APITemplate.Application.Features.ProductReview.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Returns all reviews for a specific product, ordered by creation date descending.</summary>
public sealed record GetProductReviewsByProductIdQuery(Guid ProductId);

/// <summary>Handles <see cref="GetProductReviewsByProductIdQuery"/>.</summary>
public sealed class GetProductReviewsByProductIdQueryHandler
{
    public static async Task<IReadOnlyList<ProductReviewResponse>> HandleAsync(
        GetProductReviewsByProductIdQuery request,
        IProductReviewRepository reviewRepository,
        CancellationToken ct
    ) =>
        await reviewRepository.ListAsync(
            new ProductReviewByProductIdSpecification(request.ProductId),
            ct
        );
}
