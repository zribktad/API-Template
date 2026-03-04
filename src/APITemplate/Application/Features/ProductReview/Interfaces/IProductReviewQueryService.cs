namespace APITemplate.Application.Features.ProductReview.Interfaces;

public interface IProductReviewQueryService
{
    Task<PagedResponse<ProductReviewResponse>> GetPagedAsync(ProductReviewFilter filter, CancellationToken ct = default);
    Task<ProductReviewResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductReviewResponse>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, ProductReviewResponse[]>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken ct = default);
}
