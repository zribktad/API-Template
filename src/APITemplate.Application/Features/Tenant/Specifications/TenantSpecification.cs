using APITemplate.Application.Features.Tenant.DTOs;
using APITemplate.Application.Features.Tenant.Mappings;
using Ardalis.Specification;
using TenantEntity = APITemplate.Domain.Entities.Tenant;

namespace APITemplate.Application.Features.Tenant.Specifications;

/// <summary>
/// Ardalis specification that retrieves a filtered, sorted, and paginated page of tenants projected to <see cref="TenantResponse"/>.
/// </summary>
public sealed class TenantSpecification : Specification<TenantEntity, TenantResponse>
{
    /// <summary>
    /// Initialises the specification by applying filter criteria, sort order, projection, and pagination from the given <paramref name="filter"/>.
    /// </summary>
    public TenantSpecification(TenantFilter filter)
    {
        Query.ApplyFilter(filter);
        Query.AsNoTracking();
        TenantSortFields.Map.ApplySort(Query, filter.SortBy, filter.SortDirection);
        Query.Select(TenantMappings.Projection);
    }
}
