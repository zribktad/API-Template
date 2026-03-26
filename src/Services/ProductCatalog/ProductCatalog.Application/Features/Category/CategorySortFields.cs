using SharedKernel.Application.Sorting;
using CategoryEntity = ProductCatalog.Domain.Entities.Category;

namespace ProductCatalog.Application.Features.Category;

/// <summary>
/// Defines the allowed sort fields for category queries and maps them to entity expressions.
/// </summary>
public static class CategorySortFields
{
    public static readonly SortField Name = new("name");
    public static readonly SortField CreatedAt = new("createdAt");

    public static readonly SortFieldMap<CategoryEntity> Map = new SortFieldMap<CategoryEntity>()
        .Add(Name, c => c.Name)
        .Add(CreatedAt, c => c.Audit.CreatedAtUtc)
        .Default(c => c.Audit.CreatedAtUtc);
}
