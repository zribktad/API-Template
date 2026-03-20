using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.ProductReview.Mappings;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Returns a single product review by its unique identifier, or <see langword="null"/> if not found.</summary>
public sealed record GetProductReviewByIdQuery(Guid Id) : IQuery<ProductReviewResponse?>;

/// <summary>Handles <see cref="GetProductReviewByIdQuery"/>.</summary>
public sealed class GetProductReviewByIdQueryHandler
    : IQueryHandler<GetProductReviewByIdQuery, ProductReviewResponse?>
{
    private readonly IProductReviewRepository _reviewRepository;

    public GetProductReviewByIdQueryHandler(IProductReviewRepository reviewRepository) =>
        _reviewRepository = reviewRepository;

    public async Task<ProductReviewResponse?> HandleAsync(
        GetProductReviewByIdQuery request,
        CancellationToken ct
    )
    {
        var item = await _reviewRepository.GetByIdAsync(request.Id, ct);
        return item?.ToResponse();
    }
}
