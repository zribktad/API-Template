using APITemplate.Application.Common.Security;
using HotChocolate.Authorization;
using Wolverine;

namespace APITemplate.Api.GraphQL.Mutations;

/// <summary>
/// Hot Chocolate mutation type extension that adds product-review write operations
/// (create and delete) to the <see cref="ProductMutations"/> root type.
/// </summary>
[Authorize]
[ExtendObjectType(typeof(ProductMutations))]
public class ProductReviewMutations
{
    /// <summary>Creates a new product review and returns the persisted review.</summary>
    [Authorize(Policy = Permission.ProductReviews.Create)]
    public Task<ProductReviewResponse> CreateProductReview(
        CreateProductReviewRequest input,
        [Service] IMessageBus bus,
        CancellationToken ct
    ) => bus.InvokeAsync<ProductReviewResponse>(new CreateProductReviewCommand(input), ct);

    /// <summary>Deletes a product review by its ID and returns <see langword="true"/> on success.</summary>
    [Authorize(Policy = Permission.ProductReviews.Delete)]
    public async Task<bool> DeleteProductReview(
        Guid id,
        [Service] IMessageBus bus,
        CancellationToken ct
    )
    {
        await bus.InvokeAsync(new DeleteProductReviewCommand(id), ct);
        return true;
    }
}
