using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace APITemplate.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository
    : RepositoryBase<PasswordResetToken>,
        IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(AppDbContext dbContext)
        : base(dbContext) { }

    public Task<PasswordResetToken?> GetValidByTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default
    ) =>
        AppDb.PasswordResetTokens.FirstOrDefaultAsync(
            t => t.TokenHash == tokenHash && !t.IsUsed,
            ct
        );

    public Task InvalidateAllForUserAsync(Guid userId, CancellationToken ct = default) =>
        AppDb
            .PasswordResetTokens.Where(t => t.UserId == userId && !t.IsUsed)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsUsed, true), ct);
}
