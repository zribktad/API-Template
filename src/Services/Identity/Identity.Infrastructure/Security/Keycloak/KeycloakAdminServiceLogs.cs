using Microsoft.Extensions.Logging;
using SharedKernel.Infrastructure.Logging;

namespace Identity.Infrastructure.Security.Keycloak;

internal static partial class KeycloakAdminServiceLogs
{
    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Information,
        Message = "Created Keycloak user {Username} with id {KeycloakUserId}"
    )]
    public static partial void UserCreated(
        this ILogger logger,
        [PersonalData] string username,
        [SensitiveData] string keycloakUserId
    );

    [LoggerMessage(
        EventId = 2102,
        Level = LogLevel.Warning,
        Message = "Failed to send setup email for Keycloak user {KeycloakUserId}. User was created but has no setup email."
    )]
    public static partial void SetupEmailFailed(
        this ILogger logger,
        Exception exception,
        [SensitiveData] string keycloakUserId
    );

    [LoggerMessage(
        EventId = 2103,
        Level = LogLevel.Information,
        Message = "Sent password reset email to Keycloak user {KeycloakUserId}"
    )]
    public static partial void PasswordResetEmailSent(
        this ILogger logger,
        [SensitiveData] string keycloakUserId
    );

    [LoggerMessage(
        EventId = 2104,
        Level = LogLevel.Information,
        Message = "Set Keycloak user {KeycloakUserId} enabled={Enabled}"
    )]
    public static partial void UserEnabledStateChanged(
        this ILogger logger,
        [SensitiveData] string keycloakUserId,
        bool enabled
    );

    [LoggerMessage(
        EventId = 2105,
        Level = LogLevel.Warning,
        Message = "Keycloak user {KeycloakUserId} was not found during delete — treating as already deleted."
    )]
    public static partial void UserDeleteNotFound(
        this ILogger logger,
        [SensitiveData] string keycloakUserId
    );

    [LoggerMessage(
        EventId = 2106,
        Level = LogLevel.Information,
        Message = "Deleted Keycloak user {KeycloakUserId}"
    )]
    public static partial void UserDeleted(
        this ILogger logger,
        [SensitiveData] string keycloakUserId
    );
}

internal static partial class KeycloakAdminTokenProviderLogs
{
    [LoggerMessage(
        EventId = 2201,
        Level = LogLevel.Error,
        Message = "Failed to acquire Keycloak admin token. Status: {Status}, Body: {Body}"
    )]
    public static partial void TokenAcquireFailed(
        this ILogger logger,
        int status,
        [SensitiveData] string body
    );
}
