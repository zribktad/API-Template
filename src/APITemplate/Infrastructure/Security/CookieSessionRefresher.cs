using System.Net.Http.Json;
using System.Text.Json.Serialization;
using APITemplate.Application.Common.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.Security;

internal static class CookieSessionRefresher
{
    public static async Task OnValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var expiresAtStr = context.Properties.GetTokenValue("expires_at");
        if (expiresAtStr is null || !DateTimeOffset.TryParse(expiresAtStr, out var expiresAt))
            return;

        var bffOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<BffOptions>>().Value;

        if (expiresAt - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(bffOptions.TokenRefreshThresholdMinutes))
            return;

        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        var keycloakOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<KeycloakOptions>>().Value;
        var authority = KeycloakUrlHelper.BuildAuthority(keycloakOptions.AuthServerUrl, keycloakOptions.Realm);
        var tokenEndpoint = $"{authority.TrimEnd('/')}/protocol/openid-connect/token";

        var httpClientFactory = context.HttpContext.RequestServices
            .GetRequiredService<IHttpClientFactory>();
        using var client = httpClientFactory.CreateClient("KeycloakTokenClient");

        HttpResponseMessage response;
        try
        {
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = keycloakOptions.Resource,
                ["client_secret"] = keycloakOptions.Credentials.Secret,
                ["refresh_token"] = refreshToken
            });
            response = await client.PostAsync(tokenEndpoint, form);
        }
        catch
        {
            context.RejectPrincipal();
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            context.RejectPrincipal();
            return;
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (tokenResponse is null)
        {
            context.RejectPrincipal();
            return;
        }

        context.Properties.UpdateTokenValue("access_token", tokenResponse.AccessToken);
        context.Properties.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken ?? refreshToken);
        context.Properties.UpdateTokenValue("expires_at",
            DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o"));
        context.ShouldRenew = true;
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
