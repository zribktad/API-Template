using System.Net.Http.Json;
using System.Text.Json.Serialization;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Common.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.Security;

internal static class CookieSessionRefresher
{
    public static async Task OnValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var expiresAtStr = context.Properties.GetTokenValue(AuthConstants.CookieTokenNames.ExpiresAt);
        if (expiresAtStr is null || !DateTimeOffset.TryParse(expiresAtStr, out var expiresAt))
            return;

        var bffOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<BffOptions>>().Value;

        if (expiresAt - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(bffOptions.TokenRefreshThresholdMinutes))
            return;

        var refreshToken = context.Properties.GetTokenValue(AuthConstants.CookieTokenNames.RefreshToken);
        if (string.IsNullOrEmpty(refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        var keycloakOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<KeycloakOptions>>().Value;
        var authority = KeycloakUrlHelper.BuildAuthority(keycloakOptions.AuthServerUrl, keycloakOptions.Realm);
        var tokenEndpoint = $"{authority.TrimEnd('/')}/{AuthConstants.OpenIdConnect.TokenEndpointPath}";

        var httpClientFactory = context.HttpContext.RequestServices
            .GetRequiredService<IHttpClientFactory>();
        using var client = httpClientFactory.CreateClient(AuthConstants.HttpClients.KeycloakToken);

        var ct = context.HttpContext.RequestAborted;

        HttpResponseMessage response;
        try
        {
            var formParams = new Dictionary<string, string>
            {
                [AuthConstants.OAuth2FormParameters.GrantType] = AuthConstants.OAuth2GrantTypes.RefreshToken,
                [AuthConstants.OAuth2FormParameters.ClientId] = keycloakOptions.Resource,
                [AuthConstants.OAuth2FormParameters.RefreshToken] = refreshToken
            };
            if (!string.IsNullOrEmpty(keycloakOptions.Credentials.Secret))
                formParams[AuthConstants.OAuth2FormParameters.ClientSecret] = keycloakOptions.Credentials.Secret;
            var form = new FormUrlEncodedContent(formParams);
            response = await client.PostAsync(tokenEndpoint, form, ct);
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(CookieSessionRefresher));
            logger.LogWarning(ex, "Token refresh failed, rejecting principal.");
            context.RejectPrincipal();
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            context.RejectPrincipal();
            return;
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        if (tokenResponse is null)
        {
            context.RejectPrincipal();
            return;
        }

        context.Properties.UpdateTokenValue(AuthConstants.CookieTokenNames.AccessToken, tokenResponse.AccessToken);
        context.Properties.UpdateTokenValue(AuthConstants.CookieTokenNames.RefreshToken, tokenResponse.RefreshToken ?? refreshToken);
        context.Properties.UpdateTokenValue(AuthConstants.CookieTokenNames.ExpiresAt,
            DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o"));
        context.ShouldRenew = true;
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName(AuthConstants.CookieTokenNames.AccessToken)] string AccessToken,
        [property: JsonPropertyName(AuthConstants.CookieTokenNames.RefreshToken)] string? RefreshToken,
        [property: JsonPropertyName(AuthConstants.CookieTokenNames.ExpiresIn)] int ExpiresIn);
}
