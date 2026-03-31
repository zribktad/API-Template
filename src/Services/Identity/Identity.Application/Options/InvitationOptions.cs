using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Options;

/// <summary>
/// Configuration for tenant invitation token expiry.
/// </summary>
public sealed class InvitationOptions
{
    public const string SectionName = "Invitation";

    [Range(1, 720)]
    public int InvitationTokenExpiryHours { get; init; } = 72;
}
