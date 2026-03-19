using APITemplate.Application.Features.TenantInvitation.DTOs;
using APITemplate.Application.Features.TenantInvitation.Mappings;
using APITemplate.Domain.Entities;
using Ardalis.Specification;
using TenantInvitationEntity = APITemplate.Domain.Entities.TenantInvitation;

namespace APITemplate.Application.Features.TenantInvitation.Specifications;

/// <summary>
/// Ardalis specification that retrieves a filtered, paginated page of tenant invitations projected to <see cref="TenantInvitationResponse"/>.
/// </summary>
public sealed class TenantInvitationFilterSpecification
    : Specification<TenantInvitationEntity, TenantInvitationResponse>
{
    /// <summary>
    /// Initialises the specification by applying filter criteria, descending creation-date ordering, projection, and pagination.
    /// </summary>
    public TenantInvitationFilterSpecification(TenantInvitationFilter filter)
    {
        Query.ApplyFilter(filter);
        Query.AsNoTracking();
        Query.OrderByDescending(i => i.Audit.CreatedAtUtc);
        Query.Select(TenantInvitationMappings.Projection);
        Query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize);
    }
}

/// <summary>
/// Ardalis specification used exclusively for counting tenant invitations that satisfy a given filter.
/// </summary>
public sealed class TenantInvitationCountSpecification : Specification<TenantInvitationEntity>
{
    /// <summary>
    /// Initialises the specification with the shared filter criteria applied for counting.
    /// </summary>
    public TenantInvitationCountSpecification(TenantInvitationFilter filter)
    {
        Query.ApplyFilter(filter);
    }
}

/// <summary>
/// Internal extension that applies shared <see cref="TenantInvitationFilter"/> criteria to an Ardalis specification builder.
/// </summary>
internal static class TenantInvitationFilterCriteria
{
    /// <summary>
    /// Adds optional email (normalised, case-insensitive contains) and status equality predicates to the query.
    /// </summary>
    public static void ApplyFilter(
        this ISpecificationBuilder<TenantInvitationEntity> query,
        TenantInvitationFilter filter
    )
    {
        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            var normalized = AppUser.NormalizeEmail(filter.Email);
            query.Where(i => i.Email.ToUpper().Contains(normalized));
        }

        if (filter.Status.HasValue)
            query.Where(i => i.Status == filter.Status.Value);
    }
}
