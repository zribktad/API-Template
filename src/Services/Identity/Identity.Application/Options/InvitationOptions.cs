namespace Identity.Application.Options;

/// <summary>
/// Configuration for tenant invitation token expiry.
/// </summary>
public sealed class InvitationOptions
{
    public int InvitationTokenExpiryHours { get; set; } = 72;
}
