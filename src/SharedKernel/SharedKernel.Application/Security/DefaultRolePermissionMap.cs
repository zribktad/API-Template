namespace SharedKernel.Application.Security;

public sealed class DefaultRolePermissionMap : IRolePermissionMap
{
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>(
        StringComparer.Ordinal
    );

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Map = BuildMap();

    public IReadOnlySet<string> GetPermissions(string role) =>
        Map.TryGetValue(role, out IReadOnlySet<string>? permissions) ? permissions : Empty;

    public bool HasPermission(string role, string permission) =>
        GetPermissions(role).Contains(permission);

    private static Dictionary<string, IReadOnlySet<string>> BuildMap()
    {
        HashSet<string> tenantAdminPermissions = new(StringComparer.Ordinal)
        {
            Permission.Products.Read,
            Permission.Products.Create,
            Permission.Products.Update,
            Permission.Products.Delete,
            Permission.Categories.Read,
            Permission.Categories.Create,
            Permission.Categories.Update,
            Permission.Categories.Delete,
            Permission.ProductReviews.Read,
            Permission.ProductReviews.Create,
            Permission.ProductReviews.Delete,
            Permission.ProductData.Read,
            Permission.ProductData.Create,
            Permission.ProductData.Delete,
            Permission.Users.Read,
            Permission.Users.Create,
            Permission.Users.Update,
            Permission.Users.Delete,
            Permission.Tenants.Read,
            Permission.Invitations.Read,
            Permission.Invitations.Create,
            Permission.Invitations.Revoke,
            Permission.Files.Upload,
            Permission.Files.Download,
        };

        HashSet<string> userPermissions = new(StringComparer.Ordinal)
        {
            Permission.Products.Read,
            Permission.Categories.Read,
            Permission.ProductReviews.Read,
            Permission.ProductReviews.Create,
            Permission.ProductData.Read,
            Permission.Files.Download,
        };

        return new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [SharedAuthConstants.Roles.PlatformAdmin] = Permission.All,
            [SharedAuthConstants.Roles.TenantAdmin] = tenantAdminPermissions,
            [SharedAuthConstants.Roles.User] = userPermissions,
        };
    }
}
