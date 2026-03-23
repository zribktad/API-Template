using APITemplate.Application.Features.Category.Specifications;

namespace APITemplate.Application.Features.Category;

/// <summary>Returns a paginated, filtered, and sorted list of categories.</summary>
public sealed record GetCategoriesQuery(CategoryFilter Filter);

/// <summary>Handles <see cref="GetCategoriesQuery"/>.</summary>
public sealed class GetCategoriesQueryHandler
{
    public static async Task<PagedResponse<CategoryResponse>> HandleAsync(
        GetCategoriesQuery request,
        ICategoryRepository repository,
        CancellationToken ct
    )
    {
        return await repository.GetPagedAsync(
            new CategorySpecification(request.Filter),
            request.Filter.PageNumber,
            request.Filter.PageSize,
            ct
        );
    }
}
