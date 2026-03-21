using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Security;
using HotChocolate.Authorization;

namespace APITemplate.Api.GraphQL.Mutations;

/// <summary>
/// Hot Chocolate mutation type that exposes single-item product write operations
/// backed by batch CQRS handlers, enforcing per-operation authorization policies.
/// </summary>
[Authorize]
public class ProductMutations
{
    /// <summary>Creates a new product and returns the resulting product response.</summary>
    [Authorize(Policy = Permission.Products.Create)]
    public async Task<ProductResponse> CreateProduct(
        CreateProductRequest input,
        [Service] ICommandHandler<CreateProductsCommand, BatchResponse> handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(
            new CreateProductsCommand(new CreateProductsRequest([input])),
            ct
        );

        if (result.FailureCount > 0)
            throw new GraphQLException(string.Join("; ", result.Results[0].Errors ?? []));

        var productId = result.Results[0].Id!.Value;

        return new ProductResponse(
            productId,
            input.Name,
            input.Description,
            input.Price,
            input.CategoryId,
            DateTime.UtcNow,
            (input.ProductDataIds ?? []).ToArray()
        );
    }

    /// <summary>Deletes a product by its ID and returns <see langword="true"/> on success.</summary>
    [Authorize(Policy = Permission.Products.Delete)]
    public async Task<bool> DeleteProduct(
        Guid id,
        [Service] ICommandHandler<DeleteProductsCommand, BatchResponse> handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(
            new DeleteProductsCommand(new BatchDeleteRequest([id])),
            ct
        );

        if (result.FailureCount > 0)
            throw new GraphQLException(string.Join("; ", result.Results[0].Errors ?? []));

        return true;
    }
}
