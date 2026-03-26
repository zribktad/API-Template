using Ardalis.Specification;
using ProductCatalog.Application.Features.Category.DTOs;
using ProductCatalog.Application.Features.Category.Mappings;
using CategoryEntity = ProductCatalog.Domain.Entities.Category;

namespace ProductCatalog.Application.Features.Category.Specifications;

/// <summary>
/// Ardalis specification for querying a filtered and sorted list of categories projected to <see cref="CategoryResponse"/>.
/// </summary>
public sealed class CategorySpecification : Specification<CategoryEntity, CategoryResponse>
{
    public CategorySpecification(CategoryFilter filter)
    {
        Query.ApplyFilter(filter);
        Query.AsNoTracking();
        CategorySortFields.Map.ApplySort(Query, filter.SortBy, filter.SortDirection);
        Query.Select(CategoryMappings.Projection);
    }
}
