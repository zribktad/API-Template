namespace ProductCatalog.Application.Common.Errors;

/// <summary>
/// Structured error codes for the Product Catalog service.
/// </summary>
public static class ErrorCatalog
{
    public static class Products
    {
        public const string NotFound = "PROD-0404";
        public const string NotFoundMessage = "Product with id '{0}' not found.";
    }

    public static class Categories
    {
        public const string NotFound = "CAT-0404";
        public const string NotFoundMessage = "Category with id '{0}' not found.";
    }

    public static class ProductData
    {
        public const string NotFound = "PD-0404";
        public const string NotFoundMessage = "ProductData with id(s) '{0}' not found.";
    }
}
