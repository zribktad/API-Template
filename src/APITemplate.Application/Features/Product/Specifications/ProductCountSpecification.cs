using Ardalis.Specification;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product.Specifications;

/// <summary>
/// Ardalis specification used to count products matching the active filter criteria, supporting pagination metadata without loading entity data.
/// </summary>
public sealed class ProductCountSpecification : Specification<ProductEntity>
{
    public ProductCountSpecification(ProductFilter filter)
    {
        Query.ApplyFilter(filter);
        Query.AsNoTracking();
    }
}
