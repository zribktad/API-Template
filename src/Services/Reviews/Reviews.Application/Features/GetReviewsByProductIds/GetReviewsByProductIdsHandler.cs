using ErrorOr;
using Reviews.Application.Common.Responses;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Features.GetReviewsByProductIds;

/// <summary>Returns reviews grouped by product id for a batch of product identifiers.</summary>
public sealed record GetReviewsByProductIdsQuery(IReadOnlyCollection<Guid> ProductIds);

/// <summary>
/// Wolverine message handler for <see cref="GetReviewsByProductIdsQuery"/>.
/// Invoked internally via <c>bus.InvokeAsync</c> (not exposed as an HTTP endpoint).
/// </summary>
public sealed class GetReviewsByProductIdsHandler
{
    public static async Task<
        ErrorOr<IReadOnlyDictionary<Guid, ProductReviewResponse[]>>
    > HandleAsync(
        GetReviewsByProductIdsQuery request,
        IProductReviewRepository reviewRepository,
        CancellationToken ct
    )
    {
        if (request.ProductIds.Count == 0)
            return ErrorOrFactory.From<IReadOnlyDictionary<Guid, ProductReviewResponse[]>>(
                new Dictionary<Guid, ProductReviewResponse[]>()
            );

        List<ProductReviewResponse> reviews = await reviewRepository.ListAsync(
            new GetReviewsByProductIdsSpec(request.ProductIds),
            ct
        );

        Dictionary<Guid, ProductReviewResponse[]> grouped = reviews
            .GroupBy(review => review.ProductId)
            .ToDictionary(g => g.Key, g => g.ToArray());
        return ErrorOrFactory.From<IReadOnlyDictionary<Guid, ProductReviewResponse[]>>(grouped);
    }
}
