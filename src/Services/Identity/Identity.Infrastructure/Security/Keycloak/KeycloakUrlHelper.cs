using Identity.Application.Security;

namespace Identity.Infrastructure.Security.Keycloak;

/// <summary>
/// Builds well-known Keycloak URL components (authority, discovery, token endpoint) from
/// configured base URL and realm name.
/// </summary>
public static class KeycloakUrlHelper
{
    public static string BuildAuthority(string authServerUrl, string realm) =>
        $"{authServerUrl.TrimEnd('/')}/realms/{realm}";

    public static string BuildDiscoveryUrl(string authServerUrl, string realm) =>
        $"{BuildAuthority(authServerUrl, realm)}/.well-known/openid-configuration";

    public static string BuildTokenEndpoint(string authServerUrl, string realm) =>
        $"{BuildAuthority(authServerUrl, realm)}/{AuthConstants.OpenIdConnect.TokenEndpointPath}";
}
