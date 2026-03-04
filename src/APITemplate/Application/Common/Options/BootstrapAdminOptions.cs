using System.ComponentModel.DataAnnotations;

namespace APITemplate.Application.Common.Options;

public sealed class BootstrapAdminOptions
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = "admin@example.com";

    public bool IsPlatformAdmin { get; init; } = true;
}
