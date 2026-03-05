using APITemplate.Application.Features.ProductReview.Mediator;
using MediatR;

namespace APITemplate.Application.Features.ProductReview.Services;

public sealed class ProductReviewQueryService : IProductReviewQueryService
{
    private readonly IMediator _mediator;

    public ProductReviewQueryService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<PagedResponse<ProductReviewResponse>> GetPagedAsync(ProductReviewFilter filter, CancellationToken ct = default)
        => _mediator.Send(new GetProductReviewsQuery(filter), ct);

    public Task<ProductReviewResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _mediator.Send(new GetProductReviewByIdQuery(id), ct);

    public Task<IReadOnlyList<ProductReviewResponse>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => _mediator.Send(new GetProductReviewsByProductIdQuery(productId), ct);

    public Task<IReadOnlyDictionary<Guid, ProductReviewResponse[]>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken ct = default)
        => _mediator.Send(new GetProductReviewsByProductIdsQuery(productIds), ct);
}
