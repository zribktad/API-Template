using APITemplate.Domain.Exceptions;
using Ardalis.Specification;

namespace APITemplate.Application.Common.Extensions;

public static class RepositoryExtensions
{
    public static async Task<T> GetByIdOrThrowAsync<T>(
        this IRepositoryBase<T> repository,
        Guid id,
        string errorCode,
        CancellationToken ct = default
    )
        where T : class
    {
        return await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(typeof(T).Name, id, errorCode);
    }
}
