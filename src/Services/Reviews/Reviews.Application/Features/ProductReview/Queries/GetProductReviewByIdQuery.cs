using ErrorOr;
using Reviews.Application.Common.Errors;
using Reviews.Application.Features.ProductReview.DTOs;
using Reviews.Application.Features.ProductReview.Mappings;
using Reviews.Domain.Interfaces;
using SharedKernel.Domain.Entities.Contracts;

namespace Reviews.Application.Features.ProductReview.Queries;

/// <summary>Returns a single product review by its unique identifier, or <see langword="null"/> if not found.</summary>
public sealed record GetProductReviewByIdQuery(Guid Id) : IHasId;

/// <summary>Handles <see cref="GetProductReviewByIdQuery"/>.</summary>
public sealed class GetProductReviewByIdQueryHandler
{
    public static async Task<ErrorOr<ProductReviewResponse>> HandleAsync(
        GetProductReviewByIdQuery request,
        IProductReviewRepository reviewRepository,
        CancellationToken ct
    )
    {
        Domain.Entities.ProductReview? item = await reviewRepository.GetByIdAsync(request.Id, ct);
        return item is null ? DomainErrors.Reviews.NotFound(request.Id) : item.ToResponse();
    }
}
