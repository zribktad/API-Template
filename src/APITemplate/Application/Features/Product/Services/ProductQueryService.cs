using APITemplate.Application.Features.Product.Mediator;
using MediatR;

namespace APITemplate.Application.Features.Product.Services;

public sealed class ProductQueryService : IProductQueryService
{
    private readonly IMediator _mediator;

    public ProductQueryService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<PagedResponse<ProductResponse>> GetPagedAsync(ProductFilter filter, CancellationToken ct = default)
        => _mediator.Send(new GetProductsQuery(filter), ct);

    public Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _mediator.Send(new GetProductByIdQuery(id), ct);
}
