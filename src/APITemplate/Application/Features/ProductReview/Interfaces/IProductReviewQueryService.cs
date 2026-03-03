namespace APITemplate.Application.Features.ProductReview.Interfaces;

public interface IProductReviewQueryService
{
    Task<IReadOnlyList<ProductReviewResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ProductReviewResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductReviewResponse>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
}
