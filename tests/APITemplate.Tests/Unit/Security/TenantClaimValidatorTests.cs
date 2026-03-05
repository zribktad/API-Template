using System.Security.Claims;
using APITemplate.Application.Common.Security;
using APITemplate.Infrastructure.Security;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Security;

public class TenantClaimValidatorTests
{
    [Fact]
    public void HasValidTenantClaim_WithNullPrincipal_ReturnsFalse()
    {
        TenantClaimValidator.HasValidTenantClaim(null).ShouldBeFalse();
    }

    [Fact]
    public void HasValidTenantClaim_WithNoClaims_ReturnsFalse()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        TenantClaimValidator.HasValidTenantClaim(principal).ShouldBeFalse();
    }

    [Fact]
    public void HasValidTenantClaim_WithMissingTenantClaim_ReturnsFalse()
    {
        var identity = new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString())], "test");
        var principal = new ClaimsPrincipal(identity);

        TenantClaimValidator.HasValidTenantClaim(principal).ShouldBeFalse();
    }

    [Fact]
    public void HasValidTenantClaim_WithEmptyGuid_ReturnsFalse()
    {
        var identity = new ClaimsIdentity(
            [new Claim(CustomClaimTypes.TenantId, Guid.Empty.ToString())], "test");
        var principal = new ClaimsPrincipal(identity);

        TenantClaimValidator.HasValidTenantClaim(principal).ShouldBeFalse();
    }

    [Fact]
    public void HasValidTenantClaim_WithInvalidGuidFormat_ReturnsFalse()
    {
        var identity = new ClaimsIdentity(
            [new Claim(CustomClaimTypes.TenantId, "not-a-guid")], "test");
        var principal = new ClaimsPrincipal(identity);

        TenantClaimValidator.HasValidTenantClaim(principal).ShouldBeFalse();
    }

    [Fact]
    public void HasValidTenantClaim_WithWhitespaceValue_ReturnsFalse()
    {
        var identity = new ClaimsIdentity(
            [new Claim(CustomClaimTypes.TenantId, "   ")], "test");
        var principal = new ClaimsPrincipal(identity);

        TenantClaimValidator.HasValidTenantClaim(principal).ShouldBeFalse();
    }

    [Fact]
    public void HasValidTenantClaim_WithEmptyString_ReturnsFalse()
    {
        var identity = new ClaimsIdentity(
            [new Claim(CustomClaimTypes.TenantId, "")], "test");
        var principal = new ClaimsPrincipal(identity);

        TenantClaimValidator.HasValidTenantClaim(principal).ShouldBeFalse();
    }

    [Fact]
    public void HasValidTenantClaim_WithValidGuid_ReturnsTrue()
    {
        var identity = new ClaimsIdentity(
            [new Claim(CustomClaimTypes.TenantId, Guid.NewGuid().ToString())], "test");
        var principal = new ClaimsPrincipal(identity);

        TenantClaimValidator.HasValidTenantClaim(principal).ShouldBeTrue();
    }
}
