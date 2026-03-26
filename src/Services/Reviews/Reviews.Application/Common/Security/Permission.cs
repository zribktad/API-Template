namespace Reviews.Application.Common.Security;

/// <summary>
/// Permission constants for the Reviews microservice.
/// </summary>
public static class Permission
{
    /// <summary>Permissions governing product review resource access.</summary>
    public static class ProductReviews
    {
        public const string Read = "ProductReviews.Read";
        public const string Create = "ProductReviews.Create";
        public const string Delete = "ProductReviews.Delete";
    }
}
