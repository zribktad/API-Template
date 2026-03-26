using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Infrastructure.Repositories;

namespace Identity.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="TenantInvitation"/> with token hash and pending-invitation lookup methods.
/// </summary>
public sealed class TenantInvitationRepository
    : RepositoryBase<TenantInvitation>,
        ITenantInvitationRepository
{
    private readonly IdentityDbContext _identityDb;

    public TenantInvitationRepository(IdentityDbContext dbContext)
        : base(dbContext)
    {
        _identityDb = dbContext;
    }

    /// <summary>Returns a pending invitation matching the given token hash, or <c>null</c> if none is found.</summary>
    public Task<TenantInvitation?> GetValidByTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default
    ) =>
        _identityDb.TenantInvitations.FirstOrDefaultAsync(
            i => i.TokenHash == tokenHash && i.Status == InvitationStatus.Pending,
            ct
        );

    /// <summary>Returns <c>true</c> when a pending invitation already exists for the given normalized email address.</summary>
    public Task<bool> HasPendingInvitationAsync(
        string normalizedEmail,
        CancellationToken ct = default
    ) =>
        _identityDb.TenantInvitations.AnyAsync(
            i => i.NormalizedEmail == normalizedEmail && i.Status == InvitationStatus.Pending,
            ct
        );
}
