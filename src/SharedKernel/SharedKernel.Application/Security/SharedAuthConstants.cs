namespace SharedKernel.Application.Security;

/// <summary>
/// Shared authentication claim constants used across all microservices
/// for extracting identity information from JWT tokens.
/// </summary>
public static class SharedAuthConstants
{
    /// <summary>JWT claim names shared across all services.</summary>
    public static class Claims
    {
        public const string Subject = "sub";
        public const string TenantId = "tenant_id";
    }
}
