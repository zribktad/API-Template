using APITemplate.Application.Features.Category.Mediator;
using MediatR;

namespace APITemplate.Application.Features.Category.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly IMediator _mediator;

    public CategoryService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default)
        => _mediator.Send(new GetCategoriesQuery(), ct);

    public Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _mediator.Send(new GetCategoryByIdQuery(id), ct);

    public Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
        => _mediator.Send(new CreateCategoryCommand(request), ct);

    public Task UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
        => _mediator.Send(new UpdateCategoryCommand(id, request), ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => _mediator.Send(new DeleteCategoryCommand(id), ct);

    public Task<ProductCategoryStatsResponse?> GetStatsAsync(Guid id, CancellationToken ct = default)
        => _mediator.Send(new GetCategoryStatsQuery(id), ct);
}
