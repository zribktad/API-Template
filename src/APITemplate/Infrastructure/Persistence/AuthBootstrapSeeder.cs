using APITemplate.Application.Common.Options;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.Persistence;

public sealed class AuthBootstrapSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly BootstrapAdminOptions _adminOptions;
    private readonly BootstrapTenantOptions _tenantOptions;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public AuthBootstrapSeeder(
        AppDbContext dbContext,
        IOptions<BootstrapAdminOptions> adminOptions,
        IOptions<BootstrapTenantOptions> tenantOptions)
    {
        _dbContext = dbContext;
        _adminOptions = adminOptions.Value;
        _tenantOptions = tenantOptions.Value;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Code == _tenantOptions.Code, ct);

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.Empty,
                Code = _tenantOptions.Code,
                Name = _tenantOptions.Name,
                IsActive = true
            };

            _dbContext.Tenants.Add(tenant);
            await _dbContext.SaveChangesAsync(ct);
        }

        var username = _adminOptions.Username.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.TenantId == tenant.Id && u.NormalizedUsername == username,
                ct);

        var desiredRole = _adminOptions.IsPlatformAdmin ? UserRole.PlatformAdmin : UserRole.TenantUser;
        if (user is not null)
        {
            if (user.Role != desiredRole)
            {
                user.Role = desiredRole;
                await _dbContext.SaveChangesAsync(ct);
            }

            return;
        }

        user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Username = _adminOptions.Username,
            NormalizedUsername = username,
            Email = _adminOptions.Email,
            PasswordHash = string.Empty,
            IsActive = true,
            Role = desiredRole
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, _adminOptions.Password);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);
    }
}
