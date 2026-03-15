using Ardalis.Specification;
using ProductReviewEntity = APITemplate.Domain.Entities.ProductReview;

namespace APITemplate.Application.Features.ProductReview.Specifications;

public sealed class ProductReviewCountSpecification : Specification<ProductReviewEntity>
{
    public ProductReviewCountSpecification(ProductReviewFilter filter)
    {
        Query.ApplyFilter(filter);
    }
}
