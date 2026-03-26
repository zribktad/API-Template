using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel.Domain.Entities.Contracts;

namespace SharedKernel.Infrastructure.Persistence.SoftDelete;

/// <summary>
/// Orchestrates the soft-delete of an entity entry, including recursive cascade to dependents
/// discovered through registered <see cref="ISoftDeleteCascadeRule"/> implementations.
/// </summary>
public interface ISoftDeleteProcessor
{
    /// <summary>
    /// Converts the EF Core delete for <paramref name="entry"/> into a soft-delete update,
    /// then recursively soft-deletes all dependents returned by applicable cascade rules.
    /// </summary>
    Task ProcessAsync(
        DbContext dbContext,
        EntityEntry entry,
        IAuditableTenantEntity entity,
        DateTime now,
        Guid actor,
        IReadOnlyCollection<ISoftDeleteCascadeRule> softDeleteCascadeRules,
        CancellationToken cancellationToken
    );
}
