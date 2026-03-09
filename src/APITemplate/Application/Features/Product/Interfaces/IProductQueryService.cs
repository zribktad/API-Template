namespace APITemplate.Application.Features.Product.Interfaces;

public interface IProductQueryService
{
    Task<ProductsResponse> GetPagedAsync(ProductFilter filter, CancellationToken ct = default);
    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
