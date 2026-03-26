namespace ProductCatalog.Application.Common.Security;

/// <summary>
/// Permission string constants for the Product Catalog microservice.
/// </summary>
public static class Permission
{
    /// <summary>Permissions governing product resource access.</summary>
    public static class Products
    {
        public const string Read = "Products.Read";
        public const string Create = "Products.Create";
        public const string Update = "Products.Update";
        public const string Delete = "Products.Delete";
    }

    /// <summary>Permissions governing category resource access.</summary>
    public static class Categories
    {
        public const string Read = "Categories.Read";
        public const string Create = "Categories.Create";
        public const string Update = "Categories.Update";
        public const string Delete = "Categories.Delete";
    }

    /// <summary>Permissions governing supplementary product data resource access.</summary>
    public static class ProductData
    {
        public const string Read = "ProductData.Read";
        public const string Create = "ProductData.Create";
        public const string Delete = "ProductData.Delete";
    }
}
