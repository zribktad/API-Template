using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Auth.Services;
using APITemplate.Domain.Enums;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Services;

public class TokenServiceTests
{
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        var options = Options.Create(
            new JwtOptions
            {
                Secret = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 60
            });

        _sut = new TokenService(options);
    }

    [Fact]
    public void GenerateToken_ReturnsValidToken()
    {
        var result = _sut.GenerateToken(
            new AuthenticatedUser(Guid.NewGuid(), Guid.NewGuid(), "testuser", UserRole.TenantUser));

        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.ExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_TokenContainsCorrectClaims()
    {
        var user = new AuthenticatedUser(Guid.NewGuid(), Guid.NewGuid(), "testuser", UserRole.PlatformAdmin);
        var result = _sut.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.AccessToken);

        jwt.Issuer.ShouldBe("TestIssuer");
        jwt.Audiences.ShouldContain("TestAudience");
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == UserRole.PlatformAdmin.ToString());
        jwt.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.UserId.ToString());
        jwt.Claims.ShouldContain(c => c.Type == CustomClaimTypes.TenantId && c.Value == user.TenantId.ToString());
        jwt.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_ExpiresAtIsApproximately60MinutesFromNow()
    {
        var before = DateTime.UtcNow.AddMinutes(59);
        var result = _sut.GenerateToken(
            new AuthenticatedUser(Guid.NewGuid(), Guid.NewGuid(), "testuser", UserRole.TenantUser));
        var after = DateTime.UtcNow.AddMinutes(61);

        result.ExpiresAt.ShouldBeGreaterThan(before);
        result.ExpiresAt.ShouldBeLessThan(after);
    }

    [Fact]
    public void GenerateToken_DifferentUsersGetDifferentTokens()
    {
        var token1 = _sut.GenerateToken(
            new AuthenticatedUser(Guid.NewGuid(), Guid.NewGuid(), "user1", UserRole.TenantUser));
        var token2 = _sut.GenerateToken(
            new AuthenticatedUser(Guid.NewGuid(), Guid.NewGuid(), "user2", UserRole.PlatformAdmin));

        token1.AccessToken.ShouldNotBe(token2.AccessToken);
    }

}
