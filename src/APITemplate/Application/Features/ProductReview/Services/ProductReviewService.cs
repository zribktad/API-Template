using APITemplate.Application.Features.ProductReview.Mediator;
using MediatR;

namespace APITemplate.Application.Features.ProductReview.Services;

public sealed class ProductReviewService : IProductReviewService
{
    private readonly IMediator _mediator;

    public ProductReviewService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<PagedResponse<ProductReviewResponse>> GetAllAsync(ProductReviewFilter filter, CancellationToken ct = default)
        => _mediator.Send(new GetProductReviewsQuery(filter), ct);

    public Task<IReadOnlyList<ProductReviewResponse>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => _mediator.Send(new GetProductReviewsByProductIdQuery(productId), ct);

    public Task<ProductReviewResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _mediator.Send(new GetProductReviewByIdQuery(id), ct);

    public Task<ProductReviewResponse> CreateAsync(CreateProductReviewRequest request, CancellationToken ct = default)
        => _mediator.Send(new CreateProductReviewCommand(request), ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => _mediator.Send(new DeleteProductReviewCommand(id), ct);
}
