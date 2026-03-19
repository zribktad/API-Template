using Ardalis.Specification;
using ProductReviewEntity = APITemplate.Domain.Entities.ProductReview;

namespace APITemplate.Application.Features.ProductReview.Specifications;

/// <summary>
/// Ardalis specification used exclusively for counting product reviews that match a given filter.
/// Applies the same filter criteria as <see cref="ProductReviewSpecification"/> without projection or pagination.
/// </summary>
public sealed class ProductReviewCountSpecification : Specification<ProductReviewEntity>
{
    /// <summary>Initialises the specification with the provided <paramref name="filter"/>.</summary>
    public ProductReviewCountSpecification(ProductReviewFilter filter)
    {
        Query.ApplyFilter(filter);
    }
}
