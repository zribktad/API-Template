using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace SharedKernel.Api.Options;

public sealed class KeycloakReadinessOptions
{
    public const string SectionName = "Keycloak";

    [Required]
    [ConfigurationKeyName("auth-server-url")]
    public string AuthServerUrl { get; init; } = string.Empty;

    [Required]
    [ConfigurationKeyName("realm")]
    public string Realm { get; init; } = string.Empty;

    public bool SkipReadinessCheck { get; init; }

    [Range(1, 100)]
    public int ReadinessMaxRetries { get; init; } = 30;
}
