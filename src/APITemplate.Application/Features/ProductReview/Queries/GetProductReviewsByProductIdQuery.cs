using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.ProductReview.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Returns all reviews for a specific product, ordered by creation date descending.</summary>
public sealed record GetProductReviewsByProductIdQuery(Guid ProductId)
    : IQuery<IReadOnlyList<ProductReviewResponse>>;

/// <summary>Handles <see cref="GetProductReviewsByProductIdQuery"/>.</summary>
public sealed class GetProductReviewsByProductIdQueryHandler
    : IQueryHandler<GetProductReviewsByProductIdQuery, IReadOnlyList<ProductReviewResponse>>
{
    private readonly IProductReviewRepository _reviewRepository;

    public GetProductReviewsByProductIdQueryHandler(IProductReviewRepository reviewRepository) =>
        _reviewRepository = reviewRepository;

    public async Task<IReadOnlyList<ProductReviewResponse>> HandleAsync(
        GetProductReviewsByProductIdQuery request,
        CancellationToken ct
    ) =>
        await _reviewRepository.ListAsync(
            new ProductReviewByProductIdSpecification(request.ProductId),
            ct
        );
}
