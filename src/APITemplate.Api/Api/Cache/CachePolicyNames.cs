namespace APITemplate.Api.Cache;

/// <summary>
/// Centralizes the named output-cache and rate-limit policy identifiers used across the API layer.
/// </summary>
public static class CachePolicyNames
{
    public const string Products = "Products";
    public const string Categories = "Categories";
    public const string Reviews = "Reviews";
    public const string ProductData = "ProductData";
    public const string Tenants = "Tenants";
    public const string TenantInvitations = "TenantInvitations";
    public const string Users = "Users";
    public const string RateLimitPolicy = "fixed";
}
