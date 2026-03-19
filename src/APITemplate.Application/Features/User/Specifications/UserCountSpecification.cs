using APITemplate.Application.Features.User.DTOs;
using APITemplate.Domain.Entities;
using Ardalis.Specification;

namespace APITemplate.Application.Features.User.Specifications;

/// <summary>
/// Ardalis specification used exclusively for counting users that satisfy a given filter, without projecting data.
/// </summary>
public sealed class UserCountSpecification : Specification<AppUser>
{
    /// <summary>
    /// Initialises the specification with the shared filter criteria applied for counting.
    /// </summary>
    public UserCountSpecification(UserFilter filter)
    {
        Query.ApplyFilter(filter);
        Query.AsNoTracking();
    }
}
