using System.ComponentModel.DataAnnotations;

namespace Notifications.Application.Options;

/// <summary>
/// Configuration for the outbound SMTP email service, including connection settings, sender identity,
/// and retry behaviour.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public string SmtpHost { get; init; } = "localhost";

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 587;
    public bool UseSsl { get; init; } = true;

    [Required]
    [EmailAddress]
    public string SenderEmail { get; init; } = string.Empty;

    [Required]
    public string SenderName { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Password { get; init; }

    [Range(1, 720)]
    public int InvitationTokenExpiryHours { get; init; } = 72;

    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    [Range(1, 10)]
    public int MaxRetryAttempts { get; init; } = 3;

    [Range(1, 300)]
    public int RetryBaseDelaySeconds { get; init; } = 2;
}
