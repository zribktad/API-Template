using ErrorOr;
using SharedDomainErrors = SharedKernel.Application.Errors.DomainErrors;

namespace ProductCatalog.Application.Common.Errors;

/// <summary>
/// Factory methods producing <see cref="Error"/> instances for product catalog domain errors.
/// </summary>
public static class DomainErrors
{
    public static class Products
    {
        public static Error NotFound(Guid id) =>
            SharedDomainErrors.General.NotFound(ErrorCatalog.Products.NotFound, "Product", id);
    }

    public static class Categories
    {
        public static Error NotFound(Guid id) =>
            SharedDomainErrors.General.NotFound(ErrorCatalog.Categories.NotFound, "Category", id);
    }

    public static class ProductData
    {
        public static Error NotFound(Guid id) =>
            SharedDomainErrors.General.NotFound(
                ErrorCatalog.ProductData.NotFound,
                "ProductData",
                id
            );
    }
}
