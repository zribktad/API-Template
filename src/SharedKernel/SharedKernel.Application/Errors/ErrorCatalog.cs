namespace SharedKernel.Application.Errors;

/// <summary>
/// Central catalog of structured error codes shared across all services.
/// Service-specific error codes belong in their respective Application layers.
/// </summary>
public static class ErrorCatalog
{
    /// <summary>Cross-cutting error codes not tied to a specific domain concept.</summary>
    public static class General
    {
        public const string Unknown = "GEN-0001";
        public const string ValidationFailed = "GEN-0400";
        public const string PageOutOfRange = "GEN-0400-PAGE";
        public const string NotFound = "GEN-0404";
        public const string Conflict = "GEN-0409";
        public const string ConcurrencyConflict = "GEN-0409-CONCURRENCY";
    }
}
