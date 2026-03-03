using APITemplate.Application.Features.Product.Mappings;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Product.Services;

public sealed class ProductQueryService : IProductQueryService
{
    private readonly IProductRepository _repository;

    public ProductQueryService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.ListAsync(ct);
        return items.Select(p => p.ToResponse()).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        return item?.ToResponse();
    }
}
