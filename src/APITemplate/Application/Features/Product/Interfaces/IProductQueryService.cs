namespace APITemplate.Application.Features.Product.Interfaces;

public interface IProductQueryService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
