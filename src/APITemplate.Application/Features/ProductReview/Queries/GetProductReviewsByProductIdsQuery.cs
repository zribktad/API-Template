using APITemplate.Application.Features.ProductReview.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Returns reviews grouped by product id for a batch of product identifiers.</summary>
public sealed record GetProductReviewsByProductIdsQuery(IReadOnlyCollection<Guid> ProductIds);

/// <summary>Handles <see cref="GetProductReviewsByProductIdsQuery"/>.</summary>
public sealed class GetProductReviewsByProductIdsQueryHandler
{
    public static async Task<IReadOnlyDictionary<Guid, ProductReviewResponse[]>> HandleAsync(
        GetProductReviewsByProductIdsQuery request,
        IProductReviewRepository reviewRepository,
        CancellationToken ct
    )
    {
        if (request.ProductIds.Count == 0)
            return new Dictionary<Guid, ProductReviewResponse[]>();

        var reviews = await reviewRepository.ListAsync(
            new ProductReviewByProductIdsSpecification(request.ProductIds),
            ct
        );
        var lookup = reviews.ToLookup(review => review.ProductId);

        return request.ProductIds.Distinct().ToDictionary(id => id, id => lookup[id].ToArray());
    }
}
