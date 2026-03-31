namespace SharedKernel.Api.Authorization;

public sealed class PermissionAuthorizationOptions
{
    public IReadOnlyList<string> AuthenticationSchemes { get; set; } = [];
}
