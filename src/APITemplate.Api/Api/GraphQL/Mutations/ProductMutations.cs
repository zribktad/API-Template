using APITemplate.Application.Common.Security;
using HotChocolate.Authorization;
using Wolverine;

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
        [Service] IMessageBus bus,
        CancellationToken ct
    ) => bus.InvokeAsync<BatchResponse>(new CreateProductsCommand(input), ct);

    /// <summary>Deletes a single product by ID and returns <see langword="true"/> on success.</summary>
    [Authorize(Policy = Permission.Products.Delete)]
    public async Task<bool> DeleteProduct(Guid id, [Service] IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<BatchResponse>(
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
        [Service] IMessageBus bus,
        CancellationToken ct
    ) => bus.InvokeAsync<BatchResponse>(new DeleteProductsCommand(input), ct);
}
