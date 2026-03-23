using APITemplate.Application.Features.Tenant.DTOs;
using APITemplate.Application.Features.Tenant.Specifications;
using APITemplate.Domain.Entities.Contracts;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Tenant;

public sealed record GetTenantByIdQuery(Guid Id) : IHasId;

public sealed class GetTenantByIdQueryHandler
{
    public static async Task<TenantResponse?> HandleAsync(
        GetTenantByIdQuery request,
        ITenantRepository repository,
        CancellationToken ct
    ) => await repository.FirstOrDefaultAsync(new TenantByIdSpecification(request.Id), ct);
}
