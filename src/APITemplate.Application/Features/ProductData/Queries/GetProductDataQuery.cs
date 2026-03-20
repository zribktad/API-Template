using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.ProductData.Mappings;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductData;

public sealed record GetProductDataQuery(string? Type) : IQuery<List<ProductDataResponse>>;

public sealed class GetProductDataQueryHandler
    : IQueryHandler<GetProductDataQuery, List<ProductDataResponse>>
{
    private readonly IProductDataRepository _repository;

    public GetProductDataQueryHandler(IProductDataRepository repository) =>
        _repository = repository;

    public async Task<List<ProductDataResponse>> HandleAsync(
        GetProductDataQuery request,
        CancellationToken ct
    )
    {
        var items = await _repository.GetAllAsync(request.Type, ct);
        return items.Select(item => item.ToResponse()).ToList();
    }
}
