using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain.Entities.Contracts;

namespace SharedKernel.Infrastructure.Persistence.SoftDelete;

public static class SoftDeleteExtensions
{
    public static Task<int> BulkSoftDeleteAsync<TEntity>(
        this IQueryable<TEntity> query,
        Guid? actorId,
        DateTime deletedAtUtc,
        CancellationToken ct = default
    )
        where TEntity : class, ISoftDeletable
    {
        return query.ExecuteUpdateAsync(
            s =>
                s.SetProperty(e => e.IsDeleted, true)
                    .SetProperty(e => e.DeletedAtUtc, deletedAtUtc)
                    .SetProperty(e => e.DeletedBy, actorId),
            ct
        );
    }
}
