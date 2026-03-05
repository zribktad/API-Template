using APITemplate.Application.Features.ProductData.Mediator;
using MediatR;

namespace APITemplate.Application.Features.ProductData.Services;

public sealed class ProductDataService : IProductDataService
{
    private readonly IMediator _mediator;

    public ProductDataService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<ProductDataResponse?> GetByIdAsync(string id, CancellationToken ct = default)
        => _mediator.Send(new GetProductDataByIdQuery(id), ct);

    public Task<List<ProductDataResponse>> GetAllAsync(string? type = null, CancellationToken ct = default)
        => _mediator.Send(new GetProductDataQuery(type), ct);

    public Task<ProductDataResponse> CreateImageAsync(CreateImageProductDataRequest request, CancellationToken ct = default)
        => _mediator.Send(new CreateImageProductDataCommand(request), ct);

    public Task<ProductDataResponse> CreateVideoAsync(CreateVideoProductDataRequest request, CancellationToken ct = default)
        => _mediator.Send(new CreateVideoProductDataCommand(request), ct);

    public Task DeleteAsync(string id, CancellationToken ct = default)
        => _mediator.Send(new DeleteProductDataCommand(id), ct);
}
