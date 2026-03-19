using APITemplate.Application.Features.Tenant.DTOs;
using Ardalis.Specification;
using TenantEntity = APITemplate.Domain.Entities.Tenant;

namespace APITemplate.Application.Features.Tenant.Specifications;

/// <summary>
/// Ardalis specification used exclusively for counting tenants that satisfy a given filter, without projecting data.
/// </summary>
public sealed class TenantCountSpecification : Specification<TenantEntity>
{
    /// <summary>
    /// Initialises the specification with the shared filter criteria applied for counting.
    /// </summary>
    public TenantCountSpecification(TenantFilter filter)
    {
        Query.ApplyFilter(filter);
        Query.AsNoTracking();
    }
}
