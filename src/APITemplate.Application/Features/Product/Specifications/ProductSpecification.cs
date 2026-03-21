using APITemplate.Application.Features.Product.Mappings;
using Ardalis.Specification;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product.Specifications;

/// <summary>
/// Ardalis specification that applies the full product filter, sorting, pagination, and projection to produce the paged <see cref="ProductResponse"/> list.
/// </summary>
public sealed class ProductSpecification : Specification<ProductEntity, ProductResponse>
{
    public ProductSpecification(ProductFilter filter)
    {
        Query.ApplyFilter(filter);
        Query.AsNoTracking();

        ProductSortFields.Map.ApplySort(Query, filter.SortBy, filter.SortDirection);

        Query.Select(ProductMappings.Projection);
    }
}
