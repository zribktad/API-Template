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

    public static async Task<PagedResponse<TResult>> GetPagedAsync<T, TResult>(
        this IReadRepositoryBase<T> repository,
        ISpecification<T, TResult> listSpec,
        ISpecification<T> countSpec,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default
    )
        where T : class
    {
        var itemsTask = repository.ListAsync(listSpec, ct);
        var countTask = repository.CountAsync(countSpec, ct);
        return new PagedResponse<TResult>(await itemsTask, await countTask, pageNumber, pageSize);
    }
}
