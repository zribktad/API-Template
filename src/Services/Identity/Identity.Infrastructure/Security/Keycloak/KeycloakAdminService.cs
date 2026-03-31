using Identity.Application.Options;
using Identity.Application.Security;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Security.Keycloak;

/// <summary>
/// Keycloak Admin REST API client facade that wraps user lifecycle operations
/// (create, enable/disable, password reset, delete) using the Keycloak SDK.
/// </summary>
public sealed class KeycloakAdminService : IKeycloakAdminService
{
    private readonly IKeycloakUserClient _userClient;
    private readonly string _realm;
    private readonly ILogger<KeycloakAdminService> _logger;

    public KeycloakAdminService(
        IKeycloakUserClient userClient,
        IOptions<KeycloakOptions> keycloakOptions,
        ILogger<KeycloakAdminService> logger
    )
    {
        _userClient = userClient;
        _realm = keycloakOptions.Value.Realm;
        _logger = logger;
    }

    public async Task<string> CreateUserAsync(
        string username,
        string email,
        CancellationToken ct = default
    )
    {
        UserRepresentation user = new()
        {
            Username = username,
            Email = email,
            Enabled = true,
            EmailVerified = false,
        };

        using HttpResponseMessage response = await _userClient.CreateUserWithResponseAsync(
            _realm,
            user,
            ct
        );
        response.EnsureSuccessStatusCode();

        string keycloakUserId = ExtractUserIdFromLocation(response);

        _logger.UserCreated(username, keycloakUserId);

        try
        {
            await _userClient.ExecuteActionsEmailAsync(
                _realm,
                keycloakUserId,
                new ExecuteActionsEmailRequest
                {
                    Actions =
                    [
                        AuthConstants.KeycloakActions.VerifyEmail,
                        AuthConstants.KeycloakActions.UpdatePassword,
                    ],
                },
                ct
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.SetupEmailFailed(ex, keycloakUserId);
        }

        return keycloakUserId;
    }

    public async Task SendPasswordResetEmailAsync(
        string keycloakUserId,
        CancellationToken ct = default
    )
    {
        await _userClient.ExecuteActionsEmailAsync(
            _realm,
            keycloakUserId,
            new ExecuteActionsEmailRequest
            {
                Actions = [AuthConstants.KeycloakActions.UpdatePassword],
            },
            ct
        );

        _logger.PasswordResetEmailSent(keycloakUserId);
    }

    public async Task SetUserEnabledAsync(
        string keycloakUserId,
        bool enabled,
        CancellationToken ct = default
    )
    {
        UserRepresentation patch = new() { Enabled = enabled };
        await _userClient.UpdateUserAsync(_realm, keycloakUserId, patch, ct);

        _logger.UserEnabledStateChanged(keycloakUserId, enabled);
    }

    public async Task DeleteUserAsync(string keycloakUserId, CancellationToken ct = default)
    {
        try
        {
            await _userClient.DeleteUserAsync(_realm, keycloakUserId, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.UserDeleteNotFound(keycloakUserId);
            return;
        }

        _logger.UserDeleted(keycloakUserId);
    }

    private static string ExtractUserIdFromLocation(HttpResponseMessage response)
    {
        Uri location =
            response.Headers.Location
            ?? throw new InvalidOperationException(
                "Keycloak CreateUser response did not include a Location header."
            );

        string userId = location.Segments[^1].TrimEnd('/');

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException(
                $"Could not extract user ID from Keycloak Location header: {location}"
            );

        return userId;
    }
}
