namespace Notifications.Domain.Constants;

/// <summary>
/// Central registry of email subject lines used by notification handlers.
/// Centralising these strings prevents magic-string duplication across handlers.
/// </summary>
public static class EmailSubjects
{
    public const string UserRegistration = "Welcome to the platform!";
    public const string TenantInvitation = "You've been invited to join {0}!";
    public const string UserRoleChanged = "Your role has been updated";
}
