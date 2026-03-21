using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Tenant.DTOs;
using APITemplate.Application.Features.Tenant.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Tenant;

public sealed record GetTenantsQuery(TenantFilter Filter) : IQuery<PagedResponse<TenantResponse>>;

public sealed class GetTenantsQueryHandler
    : IQueryHandler<GetTenantsQuery, PagedResponse<TenantResponse>>
{
    private readonly ITenantRepository _repository;

    public GetTenantsQueryHandler(ITenantRepository repository) => _repository = repository;

    public async Task<PagedResponse<TenantResponse>> HandleAsync(
        GetTenantsQuery request,
        CancellationToken ct
    )
    {
        return await _repository.GetPagedAsync(
            new TenantSpecification(request.Filter),
            request.Filter.PageNumber,
            request.Filter.PageSize,
            ct
        );
    }
}
