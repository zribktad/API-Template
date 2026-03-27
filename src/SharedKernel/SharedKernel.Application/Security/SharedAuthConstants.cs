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

    public static class KeycloakClaims
    {
        public const string PreferredUsername = "preferred_username";
        public const string RealmAccess = "realm_access";
        public const string Roles = "roles";
        public const string ServiceAccountUsernamePrefix = "service-account-";
    }

    public static class Roles
    {
        public const string User = "User";
        public const string PlatformAdmin = "PlatformAdmin";
        public const string TenantAdmin = "TenantAdmin";
    }
}
