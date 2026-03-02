using APITemplate.Domain.Entities;

namespace APITemplate.Domain.Interfaces;

public interface IProductDataRepository
{
    Task<ProductData?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<List<ProductData>> GetAllAsync(string? type = null, CancellationToken ct = default);

    Task<ProductData> CreateAsync(ProductData productData, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}
