using FluentValidation;
using HotChocolate.Authorization;

namespace APITemplate.Api.GraphQL.Mutations;

[Authorize]
[ExtendObjectType(typeof(ProductMutations))]
public class ProductReviewMutations
{
    public async Task<ProductReviewResponse> CreateProductReview(
        CreateProductReviewRequest input,
        [Service] IProductReviewService reviewService,
        [Service] IValidator<CreateProductReviewRequest> validator,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            throw new Domain.Exceptions.ValidationException(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                ErrorCatalog.General.ValidationFailed);

        return await reviewService.CreateAsync(input, ct);
    }

    public async Task<bool> DeleteProductReview(
        Guid id,
        [Service] IProductReviewService reviewService,
        CancellationToken ct)
    {
        await reviewService.DeleteAsync(id, ct);
        return true;
    }
}
