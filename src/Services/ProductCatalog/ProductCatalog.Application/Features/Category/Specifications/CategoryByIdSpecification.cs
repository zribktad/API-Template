using Ardalis.Specification;
using ProductCatalog.Application.Features.Category.DTOs;
using ProductCatalog.Application.Features.Category.Mappings;
using CategoryEntity = ProductCatalog.Domain.Entities.Category;

namespace ProductCatalog.Application.Features.Category.Specifications;

/// <summary>
/// Ardalis specification that fetches a single category by its identifier, projected directly to <see cref="CategoryResponse"/>.
/// </summary>
public sealed class CategoryByIdSpecification : Specification<CategoryEntity, CategoryResponse>
{
    public CategoryByIdSpecification(Guid id)
    {
        Query
            .Where(category => category.Id == id)
            .AsNoTracking()
            .Select(CategoryMappings.Projection);
    }
}
