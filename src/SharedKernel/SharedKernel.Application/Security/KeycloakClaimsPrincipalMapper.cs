using System.Security.Claims;
using System.Text.Json;

namespace SharedKernel.Application.Security;

public static class KeycloakClaimsPrincipalMapper
{
    public static void MapClaims(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
            return;

        MapUsername(identity);
        MapRealmRoles(identity);
    }

    private static void MapUsername(ClaimsIdentity identity)
    {
        if (identity.FindFirst(ClaimTypes.Name) is not null)
            return;

        string? preferredUsername = identity
            .FindFirst(SharedAuthConstants.KeycloakClaims.PreferredUsername)
            ?.Value;

        if (!string.IsNullOrWhiteSpace(preferredUsername))
            identity.AddClaim(new Claim(ClaimTypes.Name, preferredUsername));
    }

    private static void MapRealmRoles(ClaimsIdentity identity)
    {
        Claim? realmAccess = identity.FindFirst(SharedAuthConstants.KeycloakClaims.RealmAccess);
        if (realmAccess is null)
            return;

        using JsonDocument document = JsonDocument.Parse(realmAccess.Value);
        if (
            !document.RootElement.TryGetProperty(
                SharedAuthConstants.KeycloakClaims.Roles,
                out JsonElement roles
            )
        )
        {
            return;
        }

        HashSet<string> existingRoles = identity
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (JsonElement role in roles.EnumerateArray())
        {
            string? value = role.GetString();
            if (!string.IsNullOrWhiteSpace(value) && existingRoles.Add(value))
                identity.AddClaim(new Claim(ClaimTypes.Role, value));
        }
    }
}
