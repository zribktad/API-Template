using System.Security.Claims;
using System.Text.Json;

namespace APITemplate.Infrastructure.Security;

public static class KeycloakClaimMapper
{
    private const string RealmAccessClaim = "realm_access";
    private const string PreferredUsernameClaim = "preferred_username";

    public static void MapKeycloakClaims(ClaimsIdentity identity)
    {
        MapUsername(identity);
        MapRealmRoles(identity);
    }

    private static void MapUsername(ClaimsIdentity identity)
    {
        if (identity.FindFirst(ClaimTypes.Name) != null)
            return;

        var preferred = identity.FindFirst(PreferredUsernameClaim);
        if (preferred != null)
            identity.AddClaim(new Claim(ClaimTypes.Name, preferred.Value));
    }

    private static void MapRealmRoles(ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst(RealmAccessClaim);
        if (realmAccess == null)
            return;

        using var doc = JsonDocument.Parse(realmAccess.Value);
        if (!doc.RootElement.TryGetProperty("roles", out var roles))
            return;

        foreach (var role in roles.EnumerateArray())
        {
            var value = role.GetString();
            if (!string.IsNullOrEmpty(value))
                identity.AddClaim(new Claim(ClaimTypes.Role, value));
        }
    }
}
