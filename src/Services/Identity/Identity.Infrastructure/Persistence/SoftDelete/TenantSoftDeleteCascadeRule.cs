using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace Identity.Infrastructure.Persistence.SoftDelete;

/// <summary>
/// Cascade rule that soft-deletes all <see cref="AppUser"/> and <see cref="TenantInvitation"/>
/// entities belonging to a <see cref="Tenant"/> when the tenant is soft-deleted.
/// </summary>
public sealed class TenantSoftDeleteCascadeRule : ISoftDeleteCascadeRule
{
    public bool CanHandle(IAuditableTenantEntity entity) => entity is Tenant;

    public async Task<IReadOnlyCollection<IAuditableTenantEntity>> GetDependentsAsync(
        DbContext dbContext,
        IAuditableTenantEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        Tenant tenant = (Tenant)entity;

        List<AppUser> users = await dbContext
            .Set<AppUser>()
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenant.TenantId && !u.IsDeleted)
            .ToListAsync(cancellationToken);

        List<TenantInvitation> invitations = await dbContext
            .Set<TenantInvitation>()
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == tenant.TenantId && !i.IsDeleted)
            .ToListAsync(cancellationToken);

        List<IAuditableTenantEntity> dependents = new(users.Count + invitations.Count);
        dependents.AddRange(users);
        dependents.AddRange(invitations);

        return dependents;
    }
}
