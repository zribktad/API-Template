namespace APITemplate.Application.Features.Category.Interfaces;

public interface ICategoryQueryService
{
    Task<PagedResponse<CategoryResponse>> GetPagedAsync(CategoryFilter filter, CancellationToken ct = default);
    Task<CategoryResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
