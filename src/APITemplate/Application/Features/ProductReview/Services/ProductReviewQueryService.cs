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

    public async Task<PagedResponse<ProductReviewResponse>> GetPagedAsync(ProductReviewFilter filter, CancellationToken ct = default)
    {
        var items = await _repository.ListAsync(new ProductReviewSpecification(filter), ct);
        var totalCount = await _repository.CountAsync(new ProductReviewCountSpecification(filter), ct);
        return new PagedResponse<ProductReviewResponse>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<ProductReviewResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        return item?.ToResponse();
    }

    public async Task<IReadOnlyList<ProductReviewResponse>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _repository.ListAsync(new ProductReviewByProductIdSpecification(productId), ct);

    public async Task<IReadOnlyDictionary<Guid, ProductReviewResponse[]>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken ct = default)
    {
        if (productIds.Count == 0)
            return new Dictionary<Guid, ProductReviewResponse[]>();

        var reviews = await _repository.ListAsync(new ProductReviewByProductIdsSpecification(productIds), ct);
        var lookup = reviews.ToLookup(r => r.ProductId);

        return productIds
            .Distinct()
            .ToDictionary(id => id, id => lookup[id].ToArray());
    }
}
