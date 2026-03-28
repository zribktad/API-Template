using Ardalis.Specification;
using Reviews.Application.Common.Mappings;
using Reviews.Application.Common.Responses;
using ProductReviewEntity = Reviews.Domain.Entities.ProductReview;

namespace Reviews.Application.Features.GetReviewsByProductId;

/// <summary>
/// Ardalis specification that retrieves all reviews for a single product, ordered by creation date descending,
/// and projected directly to <see cref="ProductReviewResponse"/>.
/// </summary>
public sealed class GetReviewsByProductIdSpec : Specification<ProductReviewEntity, ProductReviewResponse>
{
    /// <summary>Initialises the specification for the given <paramref name="productId"/>.</summary>
    public GetReviewsByProductIdSpec(Guid productId)
    {
        Query
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.Audit.CreatedAtUtc)
            .Select(ProductReviewMappings.Projection);
    }
}
