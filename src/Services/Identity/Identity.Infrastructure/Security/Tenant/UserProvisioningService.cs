using Identity.Application.Security;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain.Interfaces;

namespace Identity.Infrastructure.Security.Tenant;

/// <summary>
/// Provisions a new <see cref="AppUser"/> on first login when an accepted
/// <see cref="TenantInvitation"/> exists for the authenticated email address.
/// Idempotent: returns the existing user immediately if one is already linked
/// to the given Keycloak subject ID.
/// </summary>
public sealed class UserProvisioningService : IUserProvisioningService
{
    private readonly IdentityDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(
        IdentityDbContext db,
        IUnitOfWork unitOfWork,
        ILogger<UserProvisioningService> logger
    )
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AppUser?> ProvisionIfNeededAsync(
        string keycloakUserId,
        string email,
        string username,
        CancellationToken ct = default
    )
    {
        AppUser? existing = await _db
            .Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, ct);

        if (existing is not null)
        {
            _logger.LogDebug(
                "User provisioning skipped — AppUser already exists for KeycloakUserId={KeycloakUserId}",
                keycloakUserId
            );
            return existing;
        }

        string normalizedEmail = AppUser.NormalizeEmail(email);

        TenantInvitation? invitation = await _db
            .TenantInvitations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                i => i.NormalizedEmail == normalizedEmail && i.Status == InvitationStatus.Accepted,
                ct
            );

        if (invitation is null)
        {
            _logger.LogInformation(
                "User provisioning skipped — no accepted invitation found for email={NormalizedEmail}",
                normalizedEmail
            );
            return null;
        }

        AppUser user = new()
        {
            Username = username,
            Email = email,
            KeycloakUserId = keycloakUserId,
            TenantId = invitation.TenantId,
            IsActive = true,
            Role = UserRole.User,
        };

        try
        {
            await _db.Users.AddAsync(user, ct);
            await _unitOfWork.CommitAsync(ct);
            _logger.LogInformation(
                "Provisioned new AppUser={UserId} for KeycloakUserId={KeycloakUserId}, TenantId={TenantId}",
                user.Id,
                keycloakUserId,
                invitation.TenantId
            );
            return user;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(
                ex,
                "DbUpdateException during provisioning for {KeycloakUserId}. Re-fetching.",
                keycloakUserId
            );

            return await _db
                    .Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, ct)
                ?? throw new InvalidOperationException(
                    $"Provisioning failed for KeycloakUserId={keycloakUserId} and no existing user was found.",
                    ex
                );
        }
    }
}
