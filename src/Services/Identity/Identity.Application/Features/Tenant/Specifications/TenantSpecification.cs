using Ardalis.Specification;
using Identity.Application.Features.Tenant.DTOs;
using Identity.Application.Features.Tenant.Mappings;
using TenantEntity = Identity.Domain.Entities.Tenant;

namespace Identity.Application.Features.Tenant.Specifications;

/// <summary>
/// Ardalis specification that retrieves a filtered and sorted list of tenants projected to <see cref="TenantResponse"/>.
/// </summary>
public sealed class TenantSpecification : Specification<TenantEntity, TenantResponse>
{
    public TenantSpecification(TenantFilter filter)
    {
        Query.ApplyFilter(filter);
        Query.AsNoTracking();
        TenantSortFields.Map.ApplySort(Query, filter.SortBy, filter.SortDirection);
        Query.Select(TenantMappings.Projection);
    }
}
