namespace Notifications.Domain.Constants;

/// <summary>
/// Central registry of email template identifiers used by <see cref="Notifications.Domain.Interfaces.IEmailTemplateRenderer"/>.
/// Centralising these strings prevents magic-string duplication across notification handlers.
/// </summary>
public static class EmailTemplateNames
{
    public const string UserRegistration = "user-registration";
    public const string TenantInvitation = "tenant-invitation";
    public const string UserRoleChanged = "user-role-changed";
}
