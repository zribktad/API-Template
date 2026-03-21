using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Category.Specifications;

namespace APITemplate.Application.Features.Category;

/// <summary>Returns a paginated, filtered, and sorted list of categories.</summary>
public sealed record GetCategoriesQuery(CategoryFilter Filter)
    : IQuery<PagedResponse<CategoryResponse>>;

/// <summary>Handles <see cref="GetCategoriesQuery"/>.</summary>
public sealed class GetCategoriesQueryHandler
    : IQueryHandler<GetCategoriesQuery, PagedResponse<CategoryResponse>>
{
    private readonly ICategoryRepository _repository;

    public GetCategoriesQueryHandler(ICategoryRepository repository) => _repository = repository;

    public async Task<PagedResponse<CategoryResponse>> HandleAsync(
        GetCategoriesQuery request,
        CancellationToken ct
    )
    {
        return await _repository.GetPagedAsync(
            new CategorySpecification(request.Filter),
            request.Filter.PageNumber,
            request.Filter.PageSize,
            ct
        );
    }
}
