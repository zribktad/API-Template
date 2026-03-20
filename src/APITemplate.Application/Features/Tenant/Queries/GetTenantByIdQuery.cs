using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Tenant.DTOs;
using APITemplate.Application.Features.Tenant.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Tenant;

public sealed record GetTenantByIdQuery(Guid Id) : IQuery<TenantResponse?>;

public sealed class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantResponse?>
{
    private readonly ITenantRepository _repository;

    public GetTenantByIdQueryHandler(ITenantRepository repository) => _repository = repository;

    public async Task<TenantResponse?> HandleAsync(
        GetTenantByIdQuery request,
        CancellationToken ct
    ) => await _repository.FirstOrDefaultAsync(new TenantByIdSpecification(request.Id), ct);
}
