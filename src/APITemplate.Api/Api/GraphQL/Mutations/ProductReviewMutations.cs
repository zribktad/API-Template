using APITemplate.Application.Common.Security;
using HotChocolate.Authorization;
using MediatR;

namespace APITemplate.Api.GraphQL.Mutations;

/// <summary>
/// Hot Chocolate mutation type extension that adds product-review write operations
/// (create and delete) to the <see cref="ProductMutations"/> root type.
/// </summary>
[Authorize]
[ExtendObjectType(typeof(ProductMutations))]
public class ProductReviewMutations
{
    /// <summary>Creates a new product review via MediatR and returns the resulting review response.</summary>
    [Authorize(Policy = Permission.ProductReviews.Create)]
    public async Task<ProductReviewResponse> CreateProductReview(
        CreateProductReviewRequest input,
        [Service] ISender sender,
        CancellationToken ct
    )
    {
        return await sender.Send(new CreateProductReviewCommand(input), ct);
    }

    /// <summary>Deletes a product review by its ID and returns <see langword="true"/> on success.</summary>
    [Authorize(Policy = Permission.ProductReviews.Delete)]
    public async Task<bool> DeleteProductReview(
        Guid id,
        [Service] ISender sender,
        CancellationToken ct
    )
    {
        await sender.Send(new DeleteProductReviewCommand(id), ct);
        return true;
    }
}
