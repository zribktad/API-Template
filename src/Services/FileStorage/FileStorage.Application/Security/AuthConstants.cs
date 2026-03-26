namespace FileStorage.Application.Security;

/// <summary>
/// Shared authentication constants for the FileStorage microservice.
/// </summary>
public static class AuthConstants
{
    /// <summary>JWT claim names used to extract identity and role information from tokens.</summary>
    public static class Claims
    {
        public const string Subject = "sub";
        public const string TenantId = "tenant_id";
    }
}
