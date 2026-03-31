using ErrorOr;
using ProductCatalog.Application.Features.Category.DTOs;
using ProductCatalog.Application.Features.Category.Specifications;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Domain.Common;

namespace ProductCatalog.Application.Features.Category.Queries;

/// <summary>Returns a paginated, filtered, and sorted list of categories.</summary>
public sealed record GetCategoriesQuery(CategoryFilter Filter);

/// <summary>Handles <see cref="GetCategoriesQuery"/>.</summary>
public sealed class GetCategoriesQueryHandler
{
    public static async Task<ErrorOr<PagedResponse<CategoryResponse>>> HandleAsync(
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
