using Ardalis.Specification;
using Reviews.Application.Common.Mappings;
using Reviews.Application.Common.Responses;
using Reviews.Application.Common.Sorting;
using ProductReviewEntity = Reviews.Domain.Entities.ProductReview;

namespace Reviews.Application.Features.GetReviews;

/// <summary>
/// Ardalis specification for querying a filtered and sorted list of product reviews
/// projected to <see cref="ProductReviewResponse"/>.
/// </summary>
public sealed class ProductReviewSpecification : Specification<ProductReviewEntity, ProductReviewResponse>
{
    /// <summary>Initialises the specification by applying filter, sort, and projection from <paramref name="filter"/>.</summary>
    public ProductReviewSpecification(ProductReviewFilter filter)
    {
        Query.ApplyFilter(filter);
        ProductReviewSortFields.Map.ApplySort(Query, filter.SortBy, filter.SortDirection);
        Query.Select(ProductReviewMappings.Projection);
    }
}

/// <summary>
/// Extension methods that apply <see cref="ProductReviewFilter"/> criteria to an Ardalis specification builder.
/// Each filter field is applied conditionally, only when a value is present.
/// </summary>
internal static class ProductReviewFilterCriteria
{
    internal static void ApplyFilter(
        this ISpecificationBuilder<ProductReviewEntity> query,
        ProductReviewFilter filter
    )
    {
        if (filter.ProductId.HasValue)
            query.Where(r => r.ProductId == filter.ProductId.Value);

        if (filter.UserId.HasValue)
            query.Where(r => r.UserId == filter.UserId.Value);

        if (filter.MinRating.HasValue)
            query.Where(r => r.Rating >= filter.MinRating.Value);

        if (filter.MaxRating.HasValue)
            query.Where(r => r.Rating <= filter.MaxRating.Value);

        if (filter.CreatedFrom.HasValue)
            query.Where(r => r.Audit.CreatedAtUtc >= filter.CreatedFrom.Value);

        if (filter.CreatedTo.HasValue)
            query.Where(r => r.Audit.CreatedAtUtc <= filter.CreatedTo.Value);
    }
}
