using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Features.Tenant.DTOs;
using Identity.Application.Features.Tenant.Specifications;
using Identity.Domain.Interfaces;
using SharedKernel.Domain.Entities.Contracts;

namespace Identity.Application.Features.Tenant.Queries;

public sealed record GetTenantByIdQuery(Guid Id) : IHasId;

public sealed class GetTenantByIdQueryHandler
{
    public static async Task<ErrorOr<TenantResponse>> HandleAsync(
        GetTenantByIdQuery request,
        ITenantRepository repository,
        CancellationToken ct
    )
    {
        TenantResponse? result = await repository.FirstOrDefaultAsync(
            new TenantByIdSpecification(request.Id),
            ct
        );
        if (result is null)
            return DomainErrors.Tenants.NotFound(request.Id);

        return result;
    }
}
