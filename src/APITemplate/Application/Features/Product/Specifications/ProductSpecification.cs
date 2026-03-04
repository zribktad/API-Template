using Ardalis.Specification;
using APITemplate.Application.Common.Specifications;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product.Specifications;
public sealed class ProductSpecification : Specification<ProductEntity, ProductResponse>
{
    public ProductSpecification(ProductFilter filter)
    {
        ProductFilterCriteria.Apply(Query, filter);

        ApplySorting(Query, filter);

        Query.Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.Audit.CreatedAtUtc));

        Query.Skip((filter.PageNumber - 1) * filter.PageSize)
             .Take(filter.PageSize);
    }

    private static void ApplySorting(ISpecificationBuilder<ProductEntity> query, ProductFilter filter)
    {
        var sortBy = filter.SortBy?.Trim().ToLowerInvariant();
        var desc = !string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        switch (sortBy)
        {
            case "name":
                SpecificationSortingHelper.ApplyOrder(
                    desc,
                    () => query.OrderBy(p => p.Name),
                    () => query.OrderByDescending(p => p.Name));
                break;
            case "price":
                SpecificationSortingHelper.ApplyOrder(
                    desc,
                    () => query.OrderBy(p => p.Price),
                    () => query.OrderByDescending(p => p.Price));
                break;
            default:
                SpecificationSortingHelper.ApplyOrder(
                    desc,
                    () => query.OrderBy(p => p.Audit.CreatedAtUtc),
                    () => query.OrderByDescending(p => p.Audit.CreatedAtUtc));
                break;
        }
    }
}
