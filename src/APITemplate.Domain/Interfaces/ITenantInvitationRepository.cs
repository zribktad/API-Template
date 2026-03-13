using APITemplate.Domain.Entities;

namespace APITemplate.Domain.Interfaces;

public interface ITenantInvitationRepository : IRepository<TenantInvitation>
{
    Task<TenantInvitation?> GetValidByTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default
    );
    Task<bool> HasPendingInvitationAsync(string normalizedEmail, CancellationToken ct = default);
}
