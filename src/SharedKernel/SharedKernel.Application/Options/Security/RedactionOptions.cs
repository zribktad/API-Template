using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Application.Options.Security;

/// <summary>
/// Configuration for HMAC-based log redaction of sensitive fields.
/// </summary>
public sealed class RedactionOptions
{
    public const string SectionName = "Redaction";

    [Required]
    public string HmacKeyEnvironmentVariable { get; init; } = "APITEMPLATE_REDACTION_HMAC_KEY";

    public string HmacKey { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int KeyId { get; init; } = 1001;
}
