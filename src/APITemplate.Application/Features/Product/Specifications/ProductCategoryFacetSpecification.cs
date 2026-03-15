using Ardalis.Specification;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product.Specifications;

public sealed class ProductCategoryFacetSpecification : Specification<ProductEntity>
{
    public ProductCategoryFacetSpecification(ProductFilter filter)
    {
        Query.ApplyFilter(filter, new ProductFilterCriteriaOptions(IgnoreCategoryIds: true));

        Query.AsNoTracking();
    }
}
