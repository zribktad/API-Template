using Ardalis.Specification;
using ProductReviewEntity = APITemplate.Domain.Entities.ProductReview;

namespace APITemplate.Application.Features.ProductReview.Specifications;
public sealed class ProductReviewSpecification : Specification<ProductReviewEntity, ProductReviewResponse>
{
    public ProductReviewSpecification(ProductReviewFilter filter)
    {
        ProductReviewFilterCriteria.Apply(Query, filter);

        ApplySorting(Query, filter);

        Query.Select(r => new ProductReviewResponse(r.Id, r.ProductId, r.ReviewerName, r.Comment, r.Rating, r.Audit.CreatedAtUtc));

        Query.Skip((filter.PageNumber - 1) * filter.PageSize)
             .Take(filter.PageSize);
    }

    private static void ApplySorting(ISpecificationBuilder<ProductReviewEntity> query, ProductReviewFilter filter)
    {
        var sortBy = filter.SortBy?.Trim().ToLowerInvariant();
        var desc = !string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        switch (sortBy)
        {
            case "rating":
                ApplyOrder(
                    desc,
                    () => query.OrderBy(r => r.Rating),
                    () => query.OrderByDescending(r => r.Rating));
                break;
            case "reviewername":
            case "reviewer_name":
            case "reviewer":
                ApplyOrder(
                    desc,
                    () => query.OrderBy(r => r.ReviewerName),
                    () => query.OrderByDescending(r => r.ReviewerName));
                break;
            default:
                ApplyOrder(
                    desc,
                    () => query.OrderBy(r => r.Audit.CreatedAtUtc),
                    () => query.OrderByDescending(r => r.Audit.CreatedAtUtc));
                break;
        }
    }

    private static void ApplyOrder(bool desc, Action applyAsc, Action applyDesc)
    {
        if (desc) applyDesc();
        else applyAsc();
    }
}
