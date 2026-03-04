using APITemplate.Domain.Entities;
using APITemplate.Domain.Enums;
using APITemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using APITemplate.Application.Common.Options;

namespace APITemplate.Application.Features.Auth.Services;

public sealed class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly string _defaultTenantCode;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public UserService(
        AppDbContext dbContext,
        IOptions<BootstrapTenantOptions> bootstrapTenantOptions)
    {
        _dbContext = dbContext;
        _defaultTenantCode = bootstrapTenantOptions.Value.Code.Trim();
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken ct = default)
    {
        var (tenantCode, principalUsername) = ResolveTenantScopedUsername(username);
        var normalizedUsername = principalUsername.ToUpperInvariant();

        var user = await _dbContext.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.Tenant.Code == tenantCode)
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

    private (string TenantCode, string Username) ResolveTenantScopedUsername(string rawUsername)
    {
        var value = rawUsername.Trim();
        var separatorIndex = value.IndexOf('\\');
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
            return (_defaultTenantCode, value);

        var tenantCode = value[..separatorIndex].Trim();
        var username = value[(separatorIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(tenantCode) || string.IsNullOrWhiteSpace(username))
            return (_defaultTenantCode, value);

        return (tenantCode, username);
    }
}
