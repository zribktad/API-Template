using APITemplate.Domain.Entities;
using APITemplate.Domain.Enums;
using APITemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace APITemplate.Application.Features.Auth.Services;

public sealed class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public UserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken ct = default)
    {
        var normalizedUsername = username.Trim().ToUpperInvariant();

        var user = await _dbContext.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.NormalizedUsername == normalizedUsername)
            .Where(u => u.IsActive && !u.IsDeleted)
            .Where(u => u.Tenant.IsActive && !u.Tenant.IsDeleted)
            .OrderByDescending(u => u.Role == UserRole.PlatformAdmin)
            .ThenBy(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return null;

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verifyResult is PasswordVerificationResult.Failed)
            return null;

        return new AuthenticatedUser(user.Id, user.TenantId, user.Username, user.Role);
    }
}
