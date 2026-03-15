using Ardalis.Specification;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product.Specifications;

public sealed class ProductPriceFacetSpecification : Specification<ProductEntity>
{
    public ProductPriceFacetSpecification(ProductFilter filter)
    {
        Query.ApplyFilter(filter, new ProductFilterCriteriaOptions(IgnorePriceRange: true));

        Query.AsNoTracking();
    }
}
