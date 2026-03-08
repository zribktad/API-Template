using Ardalis.Specification;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product.Specifications;

public sealed class ProductPriceFacetSeedSpecification : Specification<ProductEntity, ProductPriceFacetSeed>
{
    public ProductPriceFacetSeedSpecification(ProductFilter filter)
    {
        ProductFilterCriteria.Apply(
            Query,
            filter,
            new ProductFilterCriteriaOptions(IgnorePriceRange: true));

        Query.AsNoTracking();
        Query.Select(p => new ProductPriceFacetSeed(p.Price));
    }
}

public sealed record ProductPriceFacetSeed(decimal Price);
