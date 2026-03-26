using System.Data;
using SharedKernel.Domain.Options;

namespace SharedKernel.Application.Options;

/// <summary>
/// Application-level defaults for database transaction settings that can be overridden per call site.
/// Consumed by infrastructure components to build consistent <see cref="TransactionOptions"/> instances.
/// </summary>
public sealed class TransactionDefaultsOptions
{
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
    public int TimeoutSeconds { get; set; } = 30;
    public bool RetryEnabled { get; set; } = true;
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Resolves the effective <see cref="TransactionOptions"/> by combining the configured defaults
    /// in this instance with the specified <paramref name="overrides"/>.
    /// </summary>
    /// <param name="overrides">
    /// Optional per-call overrides. Any <c>null</c> or unset properties on <paramref name="overrides"/>
    /// will fall back to the corresponding default value defined on this <see cref="TransactionDefaultsOptions"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="TransactionOptions"/> instance containing the resolved transaction settings.
    /// </returns>
    public TransactionOptions Resolve(TransactionOptions? overrides)
    {
        TransactionOptions resolved = new()
        {
            IsolationLevel = overrides?.IsolationLevel ?? IsolationLevel,
            TimeoutSeconds = overrides?.TimeoutSeconds ?? TimeoutSeconds,
            RetryEnabled = overrides?.RetryEnabled ?? RetryEnabled,
            RetryCount = overrides?.RetryCount ?? RetryCount,
            RetryDelaySeconds = overrides?.RetryDelaySeconds ?? RetryDelaySeconds,
        };

        ValidateNonNegative(resolved.TimeoutSeconds, nameof(TransactionOptions.TimeoutSeconds));
        ValidateNonNegative(resolved.RetryCount, nameof(TransactionOptions.RetryCount));
        ValidateNonNegative(
            resolved.RetryDelaySeconds,
            nameof(TransactionOptions.RetryDelaySeconds)
        );

        return resolved;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> when the given integer value is negative,
    /// enforcing that transaction numeric settings are always non-negative.
    /// </summary>
    private static void ValidateNonNegative(int? value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} cannot be negative."
            );
        }
    }
}
