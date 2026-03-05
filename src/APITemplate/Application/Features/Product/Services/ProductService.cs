using APITemplate.Application.Features.Product.Mediator;
using MediatR;

namespace APITemplate.Application.Features.Product.Services;

public sealed class ProductService : IProductService
{
    private readonly IMediator _mediator;

    public ProductService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _mediator.Send(new GetProductByIdQuery(id), ct);

    public Task<PagedResponse<ProductResponse>> GetAllAsync(ProductFilter filter, CancellationToken ct = default)
        => _mediator.Send(new GetProductsQuery(filter), ct);

    public Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
        => _mediator.Send(new CreateProductCommand(request), ct);

    public Task UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
        => _mediator.Send(new UpdateProductCommand(id, request), ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => _mediator.Send(new DeleteProductCommand(id), ct);
}
