using APITemplate.Application.Common.Context;
using APITemplate.Application.Features.ProductData.Mappings;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductData;

public sealed record GetProductDataByIdQuery(Guid Id) : IHasId;

public sealed class GetProductDataByIdQueryHandler
{
    public static async Task<ProductDataResponse?> HandleAsync(
        GetProductDataByIdQuery request,
        IProductDataRepository repository,
        ITenantProvider tenantProvider,
        CancellationToken ct
    )
    {
        var tenantId = tenantProvider.TenantId;
        var data = await repository.GetByIdAsync(request.Id, ct);

        if (data is null || data.TenantId != tenantId)
            return null;

        return data.ToResponse();
    }
}
