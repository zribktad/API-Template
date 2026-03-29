using System.Text.Json;
using Identity.Application.Options;
using Identity.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Security.Bff;

/// <summary>
/// Refreshes the access token on cookie validation when it is near expiry,
/// using the stored refresh token against the Keycloak token endpoint.
/// </summary>
public static class CookieSessionRefresher
{
    public static async Task OnValidatePrincipal(CookieValidatePrincipalContext context)
    {
        string? accessToken = context.Properties.GetTokenValue(
            AuthConstants.CookieTokenNames.AccessToken
        );
        string? refreshToken = context.Properties.GetTokenValue(
            AuthConstants.CookieTokenNames.RefreshToken
        );
        string? expiresAtStr = context.Properties.GetTokenValue(
            AuthConstants.CookieTokenNames.ExpiresAt
        );

        if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(expiresAtStr))
            return;

        if (!DateTimeOffset.TryParse(expiresAtStr, out DateTimeOffset expiresAt))
            return;

        BffOptions bffOptions = context
            .HttpContext.RequestServices.GetRequiredService<IOptions<BffOptions>>()
            .Value;

        if (DateTimeOffset.UtcNow.AddMinutes(bffOptions.TokenRefreshThresholdMinutes) < expiresAt)
            return;

        KeycloakOptions keycloakOptions = context
            .HttpContext.RequestServices.GetRequiredService<IOptions<KeycloakOptions>>()
            .Value;

        IHttpClientFactory httpClientFactory =
            context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();

        HttpClient client = httpClientFactory.CreateClient(AuthConstants.HttpClients.KeycloakToken);

        string tokenEndpoint =
            $"{keycloakOptions.AuthServerUrl.TrimEnd('/')}/realms/{keycloakOptions.Realm}/{AuthConstants.OpenIdConnect.TokenEndpointPath}";

        FormUrlEncodedContent requestContent = new([
            new KeyValuePair<string, string>(
                AuthConstants.OAuth2FormParameters.ClientId,
                keycloakOptions.Resource
            ),
            new KeyValuePair<string, string>(
                AuthConstants.OAuth2FormParameters.ClientSecret,
                keycloakOptions.Credentials.Secret
            ),
            new KeyValuePair<string, string>(
                AuthConstants.OAuth2FormParameters.GrantType,
                AuthConstants.OAuth2GrantTypes.RefreshToken
            ),
            new KeyValuePair<string, string>(
                AuthConstants.OAuth2FormParameters.RefreshToken,
                refreshToken
            ),
        ]);

        HttpResponseMessage response = await client.PostAsync(tokenEndpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            context.RejectPrincipal();
            return;
        }

        using JsonDocument json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync()
        );
        JsonElement root = json.RootElement;

        string newAccessToken = root.GetProperty(AuthConstants.CookieTokenNames.AccessToken)
            .GetString()!;
        string newRefreshToken = root.GetProperty(AuthConstants.CookieTokenNames.RefreshToken)
            .GetString()!;
        int expiresIn = root.GetProperty(AuthConstants.CookieTokenNames.ExpiresIn).GetInt32();

        DateTimeOffset newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        context.Properties.StoreTokens([
            new AuthenticationToken
            {
                Name = AuthConstants.CookieTokenNames.AccessToken,
                Value = newAccessToken,
            },
            new AuthenticationToken
            {
                Name = AuthConstants.CookieTokenNames.RefreshToken,
                Value = newRefreshToken,
            },
            new AuthenticationToken
            {
                Name = AuthConstants.CookieTokenNames.ExpiresAt,
                Value = newExpiresAt.ToString("o"),
            },
        ]);

        context.ShouldRenew = true;
    }
}
