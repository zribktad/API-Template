using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.ProductData.Mappings;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductData;

public sealed record GetProductDataByIdQuery(Guid Id) : IQuery<ProductDataResponse?>, IHasId;

public sealed class GetProductDataByIdQueryHandler
    : IQueryHandler<GetProductDataByIdQuery, ProductDataResponse?>
{
    private readonly IProductDataRepository _repository;
    private readonly ITenantProvider _tenantProvider;

    public GetProductDataByIdQueryHandler(
        IProductDataRepository repository,
        ITenantProvider tenantProvider
    )
    {
        _repository = repository;
        _tenantProvider = tenantProvider;
    }

    public async Task<ProductDataResponse?> HandleAsync(
        GetProductDataByIdQuery request,
        CancellationToken ct
    )
    {
        var tenantId = _tenantProvider.TenantId;
        var data = await _repository.GetByIdAsync(request.Id, ct);

        if (data is null || data.TenantId != tenantId)
            return null;

        return data.ToResponse();
    }
}
