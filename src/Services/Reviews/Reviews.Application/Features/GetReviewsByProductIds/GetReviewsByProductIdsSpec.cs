using Ardalis.Specification;
using Reviews.Application.Common.Mappings;
using Reviews.Application.Common.Responses;
using ProductReviewEntity = Reviews.Domain.Entities.ProductReview;

namespace Reviews.Application.Features.GetReviewsByProductIds;

/// <summary>
/// Ardalis specification that retrieves reviews for a collection of product ids in a single query,
/// ordered by creation date descending and projected to <see cref="ProductReviewResponse"/>.
/// </summary>
public sealed class GetReviewsByProductIdsSpec : Specification<ProductReviewEntity, ProductReviewResponse>
{
    /// <summary>Initialises the specification for the given set of <paramref name="productIds"/>.</summary>
    public GetReviewsByProductIdsSpec(IReadOnlyCollection<Guid> productIds)
    {
        Query
            .Where(r => productIds.Contains(r.ProductId))
            .OrderByDescending(r => r.Audit.CreatedAtUtc)
            .Select(ProductReviewMappings.Projection);
    }
}
