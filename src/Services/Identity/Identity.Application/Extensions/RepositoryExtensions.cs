using Ardalis.Specification;
using ErrorOr;

namespace Identity.Application.Extensions;

/// <summary>
/// Extension methods for Ardalis repository base providing ErrorOr-based lookups.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Returns the entity by <paramref name="id"/> wrapped in <see cref="ErrorOr{T}"/>,
    /// or the supplied <paramref name="notFoundError"/> when the entity does not exist.
    /// </summary>
    public static async Task<ErrorOr<T>> GetByIdOrError<T>(
        this IRepositoryBase<T> repository,
        Guid id,
        Error notFoundError,
        CancellationToken ct = default
    )
        where T : class
    {
        T? entity = await repository.GetByIdAsync(id, ct);
        return entity is null ? notFoundError : entity;
    }
}
