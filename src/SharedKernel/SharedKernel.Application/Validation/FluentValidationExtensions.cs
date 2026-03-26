using FluentValidation;
using SharedKernel.Application.Errors;

namespace SharedKernel.Application.Validation;

/// <summary>
/// Extension methods that integrate FluentValidation with the application's error-handling conventions.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Validates <paramref name="instance"/> and throws a domain
    /// <see cref="SharedKernel.Domain.Exceptions.ValidationException"/> when validation fails,
    /// aggregating all error messages into a single semicolon-delimited string.
    /// </summary>
    public static async Task ValidateAndThrowAppAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken ct = default,
        string? errorCode = null
    )
    {
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            instance,
            ct
        );
        if (!result.IsValid)
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                string.Join("; ", result.Errors.Select(e => e.ErrorMessage)),
                errorCode ?? ErrorCatalog.General.ValidationFailed
            );
    }
}
