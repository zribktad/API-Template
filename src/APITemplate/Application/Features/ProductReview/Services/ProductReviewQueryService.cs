using APITemplate.Application.Features.ProductReview.Mappings;
using APITemplate.Application.Features.ProductReview.Specifications;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductReview.Services;

public sealed class ProductReviewQueryService : IProductReviewQueryService
{
    private readonly IProductReviewRepository _repository;

    public ProductReviewQueryService(IProductReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductReviewResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repository.ListAsync(ct);
        return items.Select(r => r.ToResponse()).ToList();
    }

    public async Task<ProductReviewResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        return item?.ToResponse();
    }

    public async Task<IReadOnlyList<ProductReviewResponse>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _repository.ListAsync(new ProductReviewByProductIdSpecification(productId), ct);
}
