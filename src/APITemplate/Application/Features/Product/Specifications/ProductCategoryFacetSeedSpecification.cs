using Ardalis.Specification;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product.Specifications;

public sealed class ProductCategoryFacetSeedSpecification : Specification<ProductEntity, ProductCategoryFacetSeed>
{
    public ProductCategoryFacetSeedSpecification(ProductFilter filter)
    {
        ProductFilterCriteria.Apply(
            Query,
            filter,
            new ProductFilterCriteriaOptions(IgnoreCategoryIds: true));

        Query.AsNoTracking();
        Query.Select(p => new ProductCategoryFacetSeed(
            p.CategoryId,
            p.Category != null ? p.Category.Name : "Uncategorized"));
    }
}

public sealed record ProductCategoryFacetSeed(Guid? CategoryId, string CategoryName);
