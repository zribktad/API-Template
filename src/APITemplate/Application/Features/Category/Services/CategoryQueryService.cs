using APITemplate.Application.Features.Category.Mappings;
using APITemplate.Application.Features.Category.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Category.Services;

public sealed class CategoryQueryService : ICategoryQueryService
{
    private readonly ICategoryRepository _repository;

    public CategoryQueryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<CategoryResponse>> GetPagedAsync(CategoryFilter filter, CancellationToken ct = default)
    {
        var items = await _repository.ListAsync(new CategorySpecification(filter), ct);
        var totalCount = await _repository.CountAsync(new CategoryCountSpecification(filter), ct);
        return new PagedResponse<CategoryResponse>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.FirstOrDefaultAsync(new CategoryByIdSpecification(id), ct);
    }
}
