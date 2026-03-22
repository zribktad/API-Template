using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Security;
using HotChocolate.Authorization;

namespace APITemplate.Api.GraphQL.Mutations;

/// <summary>
/// Hot Chocolate mutation type that exposes product write operations backed by
/// batch CQRS handlers, enforcing per-operation authorization policies.
/// </summary>
[Authorize]
public class ProductMutations
{
    /// <summary>Creates one or more products and returns a batch outcome.</summary>
    [Authorize(Policy = Permission.Products.Create)]
    public Task<BatchResponse> CreateProducts(
        CreateProductsRequest input,
        [Service] ICommandHandler<CreateProductsCommand, BatchResponse> commandHandler,
        CancellationToken ct
    ) => commandHandler.HandleAsync(new CreateProductsCommand(input), ct);

    /// <summary>Deletes a single product by ID and returns <see langword="true"/> on success.</summary>
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
            throw new GraphQLException(string.Join("; ", result.Failures[0].Errors));

        return true;
    }

    /// <summary>Deletes one or more products and returns a batch outcome.</summary>
    [Authorize(Policy = Permission.Products.Delete)]
    public Task<BatchResponse> DeleteProducts(
        BatchDeleteRequest input,
        [Service] ICommandHandler<DeleteProductsCommand, BatchResponse> handler,
        CancellationToken ct
    ) => handler.HandleAsync(new DeleteProductsCommand(input), ct);
}
