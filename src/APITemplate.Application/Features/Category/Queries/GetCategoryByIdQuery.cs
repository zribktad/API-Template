using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Category.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Category;

/// <summary>Returns a single category by its unique identifier, or <see langword="null"/> if not found.</summary>
public sealed record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryResponse?>;

/// <summary>Handles <see cref="GetCategoryByIdQuery"/>.</summary>
public sealed class GetCategoryByIdQueryHandler
    : IQueryHandler<GetCategoryByIdQuery, CategoryResponse?>
{
    private readonly ICategoryRepository _repository;

    public GetCategoryByIdQueryHandler(ICategoryRepository repository) => _repository = repository;

    public async Task<CategoryResponse?> HandleAsync(
        GetCategoryByIdQuery request,
        CancellationToken ct
    ) => await _repository.FirstOrDefaultAsync(new CategoryByIdSpecification(request.Id), ct);
}
