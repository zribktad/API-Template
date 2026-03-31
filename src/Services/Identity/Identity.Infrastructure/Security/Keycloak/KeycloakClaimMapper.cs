using SharedKernel.Application.Security;

namespace Identity.Infrastructure.Security.Keycloak;

/// <summary>
/// Maps Keycloak-specific JWT claims into standard .NET claim types.
/// </summary>
public static class KeycloakClaimMapper
{
    public static void MapKeycloakClaims(System.Security.Claims.ClaimsIdentity identity) =>
        KeycloakClaimsPrincipalMapper.MapClaims(
            new System.Security.Claims.ClaimsPrincipal(identity)
        );
}
