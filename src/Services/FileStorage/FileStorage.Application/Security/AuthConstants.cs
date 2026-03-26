using SharedKernel.Application.Security;

namespace FileStorage.Application.Security;

/// <summary>
/// Authentication constants for the FileStorage microservice.
/// Shared claim names are delegated to <see cref="SharedAuthConstants"/>.
/// </summary>
public static class AuthConstants
{
    /// <summary>JWT claim names used to extract identity and role information from tokens.</summary>
    public static class Claims
    {
        public const string Subject = SharedAuthConstants.Claims.Subject;
        public const string TenantId = SharedAuthConstants.Claims.TenantId;
    }
}
