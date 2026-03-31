using System.Net.Http.Json;
using Identity.Application.Options;
using Identity.Application.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Security.Keycloak;

/// <summary>
/// Singleton service that acquires and caches a Keycloak service-account (client credentials) token.
/// </summary>
public sealed class KeycloakAdminTokenProvider : IDisposable
{
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<KeycloakOptions> _keycloakOptions;
    private readonly ILogger<KeycloakAdminTokenProvider> _logger;
    private readonly TimeProvider _timeProvider;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public KeycloakAdminTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakOptions> keycloakOptions,
        ILogger<KeycloakAdminTokenProvider> logger,
        TimeProvider timeProvider
    )
    {
        _httpClientFactory = httpClientFactory;
        _keycloakOptions = keycloakOptions;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (IsTokenValid())
            return _cachedToken!;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (IsTokenValid())
                return _cachedToken!;

            KeycloakTokenResponse response = await FetchTokenAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(response.AccessToken))
                throw new InvalidOperationException(
                    "Keycloak token endpoint returned a response with an empty access_token."
                );

            _cachedToken = response.AccessToken;
            _tokenExpiresAt = _timeProvider.GetUtcNow().AddSeconds(response.ExpiresIn);
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<KeycloakTokenResponse> FetchTokenAsync(CancellationToken cancellationToken)
    {
        KeycloakOptions keycloak = _keycloakOptions.Value;
        string tokenEndpoint = KeycloakUrlHelper.BuildTokenEndpoint(
            keycloak.AuthServerUrl,
            keycloak.Realm
        );

        using HttpClient client = _httpClientFactory.CreateClient(
            AuthConstants.HttpClients.KeycloakToken
        );
        using FormUrlEncodedContent content = new(
            new Dictionary<string, string>
            {
                [AuthConstants.OAuth2FormParameters.GrantType] = AuthConstants
                    .OAuth2GrantTypes
                    .ClientCredentials,
                [AuthConstants.OAuth2FormParameters.ClientId] = keycloak.Resource,
                [AuthConstants.OAuth2FormParameters.ClientSecret] = keycloak.Credentials.Secret,
            }
        );

        using HttpResponseMessage response = await client.PostAsync(
            tokenEndpoint,
            content,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed to acquire Keycloak admin token. Status: {Status}. Body: {Body}",
                (int)response.StatusCode,
                body
            );
            response.EnsureSuccessStatusCode();
        }

        KeycloakTokenResponse token =
            await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Keycloak token endpoint returned empty body.");

        return token;
    }

    private bool IsTokenValid() =>
        _cachedToken is not null && _timeProvider.GetUtcNow() < _tokenExpiresAt - ExpiryMargin;

    public void Dispose() => _lock.Dispose();
}
