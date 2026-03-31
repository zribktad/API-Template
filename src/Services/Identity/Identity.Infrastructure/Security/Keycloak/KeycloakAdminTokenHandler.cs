using System.Net.Http.Headers;

namespace Identity.Infrastructure.Security.Keycloak;

/// <summary>
/// A transient <see cref="DelegatingHandler"/> that attaches a cached Keycloak
/// service-account Bearer token to every outbound admin API request.
/// </summary>
public sealed class KeycloakAdminTokenHandler : DelegatingHandler
{
    private readonly KeycloakAdminTokenProvider _tokenProvider;

    public KeycloakAdminTokenHandler(KeycloakAdminTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        string token = await _tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
