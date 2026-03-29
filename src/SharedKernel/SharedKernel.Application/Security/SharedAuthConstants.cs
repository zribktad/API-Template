namespace SharedKernel.Application.Security;

/// <summary>
/// Shared authentication claim constants used across all microservices
/// for extracting identity information from JWT tokens.
/// </summary>
public static class SharedAuthConstants
{
    /// <summary>Relative path segments for Keycloak OpenID Connect endpoints.</summary>
    public static class OpenIdConnect
    {
        public const string AuthorizationEndpointPath = "protocol/openid-connect/auth";
        public const string TokenEndpointPath = "protocol/openid-connect/token";
    }

    /// <summary>OpenAPI/Scalar constants shared by service hosts and gateway.</summary>
    public static class OpenApi
    {
        public const string OAuth2Scheme = "OAuth2";
        public const string ScalarClientId = "api-template-scalar";
        public const string DefaultDocumentName = "v1";
    }

    /// <summary>Default OIDC scopes requested by interactive API tooling.</summary>
    public static class Scopes
    {
        public const string OpenId = "openid";
        public const string Profile = "profile";
        public const string Email = "email";
        public static readonly string[] Default = [OpenId, Profile, Email];
    }

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
