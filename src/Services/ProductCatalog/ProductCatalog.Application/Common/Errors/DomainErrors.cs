using ErrorOr;

namespace ProductCatalog.Application.Common.Errors;

/// <summary>
/// Factory methods producing <see cref="Error"/> instances for product catalog domain errors.
/// </summary>
public static class DomainErrors
{
    public static class Products
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: ErrorCatalog.Products.NotFound,
                description: $"Product with id '{id}' not found."
            );
    }

    public static class Categories
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: ErrorCatalog.Categories.NotFound,
                description: $"Category with id '{id}' not found."
            );
    }

    public static class ProductData
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: ErrorCatalog.ProductData.NotFound,
                description: $"ProductData with id '{id}' not found."
            );
    }
}
