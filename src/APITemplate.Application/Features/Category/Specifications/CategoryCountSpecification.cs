using Ardalis.Specification;
using CategoryEntity = APITemplate.Domain.Entities.Category;

namespace APITemplate.Application.Features.Category.Specifications;

/// <summary>
/// Ardalis specification used exclusively for counting categories that match a given filter.
/// Applies the same filter criteria as <see cref="CategorySpecification"/> without projection or pagination.
/// </summary>
public sealed class CategoryCountSpecification : Specification<CategoryEntity>
{
    /// <summary>Initialises the specification with the provided <paramref name="filter"/>.</summary>
    public CategoryCountSpecification(CategoryFilter filter)
    {
        Query.ApplyFilter(filter);
        Query.AsNoTracking();
    }
}
