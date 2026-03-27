using System.Security.Claims;
using Identity.Application.Security;
using Identity.Infrastructure.Security.Keycloak;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JwtTokenValidatedContext = Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext;
using OidcTokenValidatedContext = Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext;

namespace Identity.Infrastructure.Security.Tenant;

/// <summary>
/// Validates tenant-related claims after JWT/OIDC token validation and normalizes
/// Keycloak claims into standard .NET claim types used by authorization policies.
/// </summary>
public static class TenantClaimValidator
{
    public static async Task OnTokenValidated(JwtTokenValidatedContext context)
    {
        await ValidateTokenAsync(
            context.Principal,
            context.HttpContext,
            JwtBearerDefaults.AuthenticationScheme,
            reason => context.Fail(reason)
        );
    }

    public static async Task OnTokenValidated(OidcTokenValidatedContext context)
    {
        await ValidateTokenAsync(
            context.Principal,
            context.HttpContext,
            OpenIdConnectDefaults.AuthenticationScheme,
            reason => context.Fail(reason)
        );
    }

    private static async Task ValidateTokenAsync(
        ClaimsPrincipal? principal,
        HttpContext httpContext,
        string schemeName,
        Action<string> fail
    )
    {
        ClaimsIdentity? identity = principal?.Identity as ClaimsIdentity;
        if (identity != null)
            KeycloakClaimMapper.MapKeycloakClaims(identity);

        Domain.Entities.AppUser? user = null;
        if (!IsServiceAccount(principal))
            user = await TryProvisionUserAsync(httpContext, principal);

        if (identity is not null && user is not null)
            EnrichIdentity(identity, user);

        if (!HasValidTenantClaim(principal) && !IsServiceAccount(principal))
        {
            fail($"Missing required {AuthConstants.Claims.TenantId} claim.");
        }

        LogTokenValidated(httpContext, principal, schemeName);
    }

    public static bool HasValidTenantClaim(ClaimsPrincipal? principal)
    {
        return principal?.HasClaim(c =>
                c.Type == AuthConstants.Claims.TenantId
                && Guid.TryParse(c.Value, out Guid tenantId)
                && tenantId != Guid.Empty
            ) == true;
    }

    private static async Task<Domain.Entities.AppUser?> TryProvisionUserAsync(
        HttpContext httpContext,
        ClaimsPrincipal? principal
    )
    {
        try
        {
            string? sub = principal?.FindFirstValue(AuthConstants.Claims.Subject);
            string? email = principal?.FindFirstValue(ClaimTypes.Email);
            string? username = principal?.FindFirstValue(AuthConstants.Claims.PreferredUsername);

            if (
                string.IsNullOrEmpty(sub)
                || string.IsNullOrEmpty(email)
                || string.IsNullOrEmpty(username)
            )
                return null;

            IUserProvisioningService provisioningService =
                httpContext.RequestServices.GetRequiredService<IUserProvisioningService>();

            return await provisioningService.ProvisionIfNeededAsync(sub, email, username);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ILogger logger = httpContext
                .RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(TenantClaimValidator));

            logger.LogWarning(
                ex,
                "User provisioning failed during token validation — authentication will continue"
            );

            return null;
        }
    }

    private static void EnrichIdentity(ClaimsIdentity identity, Domain.Entities.AppUser user)
    {
        ReplaceClaim(identity, AuthConstants.Claims.TenantId, user.TenantId.ToString());
        ReplaceClaim(identity, ClaimTypes.NameIdentifier, user.Id.ToString());
    }

    private static void ReplaceClaim(ClaimsIdentity identity, string claimType, string value)
    {
        foreach (Claim existing in identity.FindAll(claimType).ToArray())
            identity.RemoveClaim(existing);

        identity.AddClaim(new Claim(claimType, value));
    }

    private static bool IsServiceAccount(ClaimsPrincipal? principal)
    {
        string? username = principal?.FindFirstValue(AuthConstants.Claims.PreferredUsername);
        return username != null
            && username.StartsWith(
                AuthConstants.Claims.ServiceAccountUsernamePrefix,
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static void LogTokenValidated(
        HttpContext httpContext,
        ClaimsPrincipal? principal,
        string scheme
    )
    {
        ILogger logger = httpContext
            .RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(TenantClaimValidator));

        if (principal?.Identity is not ClaimsIdentity identity)
        {
            logger.LogWarning("[{Scheme}] Token validated but no identity found", scheme);
            return;
        }

        string? name = identity.FindFirst(ClaimTypes.Name)?.Value;
        string[] roles = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        string? tenantId = identity.FindFirst(AuthConstants.Claims.TenantId)?.Value;

        logger.LogInformation(
            "[{Scheme}] Authenticated user={User}, tenant={TenantId}, roles=[{Roles}]",
            scheme,
            name,
            tenantId,
            string.Join(", ", roles)
        );
    }
}
