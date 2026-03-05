using System.ComponentModel.DataAnnotations;

namespace APITemplate.Application.Common.Options;

public sealed class BffOptions
{
    [Required]
    public string Authority { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    public string CookieName { get; init; } = ".APITemplate.Bff";

    public string PostLogoutRedirectUri { get; init; } = "/";

    public int SessionTimeoutMinutes { get; init; } = 60;

    public string[] Scopes { get; init; } = ["openid", "profile", "email", "api"];
}
