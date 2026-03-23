using APITemplate.Application.Features.ProductReview.Mappings;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Returns a single product review by its unique identifier, or <see langword="null"/> if not found.</summary>
public sealed record GetProductReviewByIdQuery(Guid Id) : IHasId;

/// <summary>Handles <see cref="GetProductReviewByIdQuery"/>.</summary>
public sealed class GetProductReviewByIdQueryHandler
{
    public static async Task<ProductReviewResponse?> HandleAsync(
        GetProductReviewByIdQuery request,
        IProductReviewRepository reviewRepository,
        CancellationToken ct
    )
    {
        var item = await reviewRepository.GetByIdAsync(request.Id, ct);
        return item?.ToResponse();
    }
}
