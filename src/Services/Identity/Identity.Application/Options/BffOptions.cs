using System.ComponentModel.DataAnnotations;
using Identity.Application.Security;

namespace Identity.Application.Options;

/// <summary>
/// Configuration for the Backend-for-Frontend (BFF) session layer, including cookie settings,
/// requested OIDC scopes, and token refresh thresholds.
/// </summary>
public sealed class BffOptions
{
    public const string SectionName = "Bff";

    [Required]
    public string CookieName { get; init; } = ".Identity.Auth";

    [Required]
    public string PostLogoutRedirectUri { get; init; } = "/";

    [Range(1, 1440)]
    public int SessionTimeoutMinutes { get; init; } = 60;

    [MinLength(1)]
    public string[] Scopes { get; init; } = [.. AuthConstants.Scopes.Default];

    [Range(1, 60)]
    public int TokenRefreshThresholdMinutes { get; init; } = 2;
}
