using System.Net.Http.Json;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Features.Auth.DTOs;
using APITemplate.Application.Features.Auth.Interfaces;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.Security;

public sealed class AuthentikAuthenticationProxy : IAuthenticationProxy
{
    private readonly HttpClient _httpClient;
    private readonly AuthentikOptions _options;

    public AuthentikAuthenticationProxy(HttpClient httpClient, IOptions<AuthentikOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<TokenResponse?> AuthenticateAsync(
        string username, string password, CancellationToken ct = default)
    {
        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid profile email api"
        });

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(_options.TokenEndpoint, requestBody, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
            return null;

        var tokenResult = await response.Content.ReadFromJsonAsync<AuthentikTokenResponse>(ct);
        if (tokenResult is null || string.IsNullOrEmpty(tokenResult.AccessToken))
            return null;

        return new TokenResponse(tokenResult.AccessToken, DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn));
    }
}
