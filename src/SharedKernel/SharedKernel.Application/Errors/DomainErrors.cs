using ErrorOr;

namespace SharedKernel.Application.Errors;

/// <summary>
/// Factory methods producing <see cref="Error"/> instances for cross-cutting error codes.
/// Service-specific error factories belong in their respective Application layers.
/// </summary>
public static class DomainErrors
{
    public static class General
    {
        public static Error NotFound(string entityName, Guid id) =>
            Error.NotFound(
                code: ErrorCatalog.General.NotFound,
                description: $"{entityName} with id '{id}' not found."
            );
    }
}
