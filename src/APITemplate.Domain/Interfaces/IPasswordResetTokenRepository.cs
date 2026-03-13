using APITemplate.Domain.Entities;

namespace APITemplate.Domain.Interfaces;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetValidByTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default
    );
    Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default);
}
