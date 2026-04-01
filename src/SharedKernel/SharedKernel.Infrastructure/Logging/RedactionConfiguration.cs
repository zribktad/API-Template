using SharedKernel.Application.Options.Security;

namespace SharedKernel.Infrastructure.Logging;

/// <summary>
/// Resolves the effective HMAC key used for sensitive data redaction.
/// </summary>
public static class RedactionConfiguration
{
    public static string ResolveHmacKey(
        RedactionOptions options,
        Func<string, string?> getEnvironmentVariable
    )
    {
        string? key = getEnvironmentVariable(options.HmacKeyEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(key))
            return key;

        if (!string.IsNullOrWhiteSpace(options.HmacKey))
            return options.HmacKey;

        throw new InvalidOperationException(
            $"Missing redaction HMAC key. Set environment variable '{options.HmacKeyEnvironmentVariable}' or configure 'Redaction:HmacKey'."
        );
    }
}
