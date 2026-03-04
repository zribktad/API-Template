using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using APITemplate.Application.Common.Security;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Enums;
using APITemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Username = "wrong", Password = "wrong" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Invalid username or password.");
    }

    [Fact]
    public async Task Login_BootstrapAdminToken_ContainsRoleClaim()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Username = "default\\admin", Password = "admin" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResponse.ShouldNotBeNull();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse.AccessToken);
        var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.TenantId);
        tenantClaim.ShouldNotBeNull();
        Guid.TryParse(tenantClaim.Value, out _).ShouldBeTrue();
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == UserRole.PlatformAdmin.ToString());
    }

    [Fact]
    public async Task Login_TenantUserToken_ContainsTenantUserClaims()
    {
        var tenant = await SeedTenantUserAsync("tenant.user", "tenant.user@example.com", "tenant-pass");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Username = $"{tenant.Code}\\tenant.user", Password = "tenant-pass" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResponse.ShouldNotBeNull();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse.AccessToken);
        jwt.Claims.ShouldContain(c => c.Type == CustomClaimTypes.TenantId && c.Value == tenant.Id.ToString());
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == UserRole.TenantUser.ToString());
    }

    private async Task<Tenant> SeedTenantUserAsync(string username, string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Code = $"tenant-{Guid.NewGuid():N}",
            Name = "Tenant User Tenant",
            IsActive = true
        };

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Username = username,
            Email = email,
            PasswordHash = string.Empty,
            IsActive = true,
            Role = UserRole.TenantUser
        };

        var hasher = new PasswordHasher<AppUser>();
        user.PasswordHash = hasher.HashPassword(user, password);

        dbContext.Tenants.Add(tenant);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return tenant;
    }
}

