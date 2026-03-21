using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.ProductReview.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Returns a paginated, filtered, and sorted list of product reviews.</summary>
public sealed record GetProductReviewsQuery(ProductReviewFilter Filter)
    : IQuery<PagedResponse<ProductReviewResponse>>;

/// <summary>Handles <see cref="GetProductReviewsQuery"/>.</summary>
public sealed class GetProductReviewsQueryHandler
    : IQueryHandler<GetProductReviewsQuery, PagedResponse<ProductReviewResponse>>
{
    private readonly IProductReviewRepository _reviewRepository;

    public GetProductReviewsQueryHandler(IProductReviewRepository reviewRepository) =>
        _reviewRepository = reviewRepository;

    public async Task<PagedResponse<ProductReviewResponse>> HandleAsync(
        GetProductReviewsQuery request,
        CancellationToken ct
    )
    {
        return await _reviewRepository.GetPagedAsync(
            new ProductReviewSpecification(request.Filter),
            request.Filter.PageNumber,
            request.Filter.PageSize,
            ct
        );
    }
}
