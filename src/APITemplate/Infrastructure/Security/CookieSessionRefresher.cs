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
        if (!TryGetExpiration(context, out var expiresAt))
            return;

        if (!IsRefreshRequired(context, expiresAt))
            return;

        if (!TryGetRefreshToken(context, out var refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        var tokenResponse = await TryRefreshSessionAsync(context, refreshToken);
        if (tokenResponse is null)
        {
            context.RejectPrincipal();
            return;
        }

        ApplyRefreshedSession(context, tokenResponse, refreshToken);
    }

    private static bool TryGetExpiration(
        CookieValidatePrincipalContext context,
        out DateTimeOffset expiresAt)
    {
        expiresAt = default;
        var expiresAtStr = context.Properties.GetTokenValue(AuthConstants.CookieTokenNames.ExpiresAt);
        return expiresAtStr is not null
            && DateTimeOffset.TryParse(expiresAtStr, out expiresAt);
    }

    private static bool IsRefreshRequired(
        CookieValidatePrincipalContext context,
        DateTimeOffset expiresAt)
    {
        var bffOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<BffOptions>>().Value;

        return expiresAt - DateTimeOffset.UtcNow
            <= TimeSpan.FromMinutes(bffOptions.TokenRefreshThresholdMinutes);
    }

    private static bool TryGetRefreshToken(
        CookieValidatePrincipalContext context,
        out string refreshToken)
    {
        refreshToken = context.Properties.GetTokenValue(AuthConstants.CookieTokenNames.RefreshToken) ?? string.Empty;
        return !string.IsNullOrEmpty(refreshToken);
    }

    private static async Task<TokenResponse?> TryRefreshSessionAsync(
        CookieValidatePrincipalContext context,
        string refreshToken)
    {
        var keycloakOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<KeycloakOptions>>().Value;
        var tokenEndpoint = BuildTokenEndpoint(keycloakOptions);

        using var client = context.HttpContext.RequestServices
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(AuthConstants.HttpClients.KeycloakToken);

        try
        {
            using var response = await client.PostAsync(
                tokenEndpoint,
                BuildRefreshRequestContent(keycloakOptions, refreshToken),
                context.HttpContext.RequestAborted);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<TokenResponse>(context.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            GetLogger(context).LogWarning(ex, "Token refresh failed, rejecting principal.");
            return null;
        }
    }

    private static string BuildTokenEndpoint(KeycloakOptions keycloakOptions)
    {
        var authority = KeycloakUrlHelper.BuildAuthority(keycloakOptions.AuthServerUrl, keycloakOptions.Realm);
        return $"{authority.TrimEnd('/')}/{AuthConstants.OpenIdConnect.TokenEndpointPath}";
    }

    private static FormUrlEncodedContent BuildRefreshRequestContent(
        KeycloakOptions keycloakOptions,
        string refreshToken)
    {
        var formParams = new Dictionary<string, string>
        {
            [AuthConstants.OAuth2FormParameters.GrantType] = AuthConstants.OAuth2GrantTypes.RefreshToken,
            [AuthConstants.OAuth2FormParameters.ClientId] = keycloakOptions.Resource,
            [AuthConstants.OAuth2FormParameters.RefreshToken] = refreshToken
        };

        if (!string.IsNullOrEmpty(keycloakOptions.Credentials.Secret))
            formParams[AuthConstants.OAuth2FormParameters.ClientSecret] = keycloakOptions.Credentials.Secret;

        return new FormUrlEncodedContent(formParams);
    }

    private static void ApplyRefreshedSession(
        CookieValidatePrincipalContext context,
        TokenResponse tokenResponse,
        string refreshToken)
    {
        context.Properties.UpdateTokenValue(AuthConstants.CookieTokenNames.AccessToken, tokenResponse.AccessToken);
        context.Properties.UpdateTokenValue(
            AuthConstants.CookieTokenNames.RefreshToken,
            tokenResponse.RefreshToken ?? refreshToken);
        context.Properties.UpdateTokenValue(
            AuthConstants.CookieTokenNames.ExpiresAt,
            DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o"));
        context.ShouldRenew = true;
    }

    private static ILogger GetLogger(CookieValidatePrincipalContext context)
    {
        return context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(CookieSessionRefresher));
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName(AuthConstants.CookieTokenNames.AccessToken)] string AccessToken,
        [property: JsonPropertyName(AuthConstants.CookieTokenNames.RefreshToken)] string? RefreshToken,
        [property: JsonPropertyName(AuthConstants.CookieTokenNames.ExpiresIn)] int ExpiresIn);
}
