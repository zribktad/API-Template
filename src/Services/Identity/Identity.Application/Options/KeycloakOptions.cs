using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Identity.Application.Options;

/// <summary>
/// Configuration for the Keycloak identity provider, covering realm, server URL, client credentials,
/// and startup readiness-check behaviour.
/// </summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    [Required]
    [ConfigurationKeyName("realm")]
    public string Realm { get; init; } = string.Empty;

    [Required]
    [ConfigurationKeyName("auth-server-url")]
    public string AuthServerUrl { get; init; } = string.Empty;

    [ConfigurationKeyName("resource")]
    public string Resource { get; init; } = string.Empty;

    [ConfigurationKeyName("SkipReadinessCheck")]
    public bool SkipReadinessCheck { get; init; }

    [Range(1, 100)]
    [ConfigurationKeyName("ReadinessMaxRetries")]
    public int ReadinessMaxRetries { get; init; } = 30;

    [Required]
    [ValidateObjectMembers]
    [ConfigurationKeyName("credentials")]
    public KeycloakCredentialsOptions Credentials { get; init; } = new();
}

/// <summary>
/// Client-secret credentials used when authenticating against the Keycloak Admin REST API.
/// </summary>
public sealed class KeycloakCredentialsOptions
{
    [Required]
    [ConfigurationKeyName("secret")]
    public string Secret { get; init; } = string.Empty;
}
