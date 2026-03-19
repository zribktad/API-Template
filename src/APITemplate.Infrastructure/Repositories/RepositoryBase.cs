using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;

namespace APITemplate.Infrastructure.Repositories;

/// <summary>
/// Base repository that wraps the Ardalis Specification EF Core repository, overriding write methods
/// to stage changes without flushing — persistence is deferred to <see cref="IUnitOfWork.CommitAsync"/>.
/// </summary>
// Generic base repository — T is constrained to class (reference type) so EF Core can track it.
// abstract = cannot be instantiated directly, must be inherited (e.g. ProductRepository : RepositoryBase<Product>).
// SaveChangesAsync is intentionally NOT called here — use IUnitOfWork.CommitAsync() in the service layer.
public abstract class RepositoryBase<T>
    : Ardalis.Specification.EntityFrameworkCore.RepositoryBase<T>,
        IRepository<T>
    where T : class
{
    // Cast to AppDbContext — Ardalis exposes DbContext as the base DbContext type
    protected AppDbContext AppDb => (AppDbContext)DbContext;

    protected RepositoryBase(AppDbContext dbContext)
        : base(dbContext) { }

    // Override write methods — do NOT call SaveChangesAsync, that is UoW responsibility.
    // Return 0 (no rows persisted yet — UoW will commit later).
    /// <summary>Tracks <paramref name="entity"/> for insertion without flushing to the database.</summary>
    public override Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        DbContext.Set<T>().Add(entity);
        return Task.FromResult(entity);
    }

    /// <summary>Marks <paramref name="entity"/> as modified without flushing to the database.</summary>
    public override Task<int> UpdateAsync(T entity, CancellationToken ct = default)
    {
        DbContext.Set<T>().Update(entity);
        return Task.FromResult(0);
    }

    // Guid-based delete (our contract, not in IRepositoryBase)
    /// <summary>
    /// Looks up the entity by <paramref name="id"/> and marks it for deletion.
    /// Throws <see cref="Domain.Exceptions.NotFoundException"/> when the entity does not exist.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default, string? errorCode = null)
    {
        var entity =
            await GetByIdAsync(id, ct)
            ?? throw new NotFoundException(
                typeof(T).Name,
                id,
                errorCode ?? ErrorCatalog.General.NotFound
            );
        DbContext.Set<T>().Remove(entity);
    }
}
