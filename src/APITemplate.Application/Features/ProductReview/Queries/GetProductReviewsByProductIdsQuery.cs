using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.ProductReview.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Returns reviews grouped by product id for a batch of product identifiers.</summary>
public sealed record GetProductReviewsByProductIdsQuery(IReadOnlyCollection<Guid> ProductIds)
    : IQuery<IReadOnlyDictionary<Guid, ProductReviewResponse[]>>;

/// <summary>Handles <see cref="GetProductReviewsByProductIdsQuery"/>.</summary>
public sealed class GetProductReviewsByProductIdsQueryHandler
    : IQueryHandler<
        GetProductReviewsByProductIdsQuery,
        IReadOnlyDictionary<Guid, ProductReviewResponse[]>
    >
{
    private readonly IProductReviewRepository _reviewRepository;

    public GetProductReviewsByProductIdsQueryHandler(IProductReviewRepository reviewRepository) =>
        _reviewRepository = reviewRepository;

    public async Task<IReadOnlyDictionary<Guid, ProductReviewResponse[]>> HandleAsync(
        GetProductReviewsByProductIdsQuery request,
        CancellationToken ct
    )
    {
        if (request.ProductIds.Count == 0)
            return new Dictionary<Guid, ProductReviewResponse[]>();

        var reviews = await _reviewRepository.ListAsync(
            new ProductReviewByProductIdsSpecification(request.ProductIds),
            ct
        );
        var lookup = reviews.ToLookup(review => review.ProductId);

        return request.ProductIds.Distinct().ToDictionary(id => id, id => lookup[id].ToArray());
    }
}
