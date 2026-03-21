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
    /// <summary>Creates a new product; returns the same batch summary shape as the REST create endpoint.</summary>
    [Authorize(Policy = Permission.Products.Create)]
    public Task<BatchResponse> CreateProduct(
        CreateProductRequest input,
        [Service] ICommandHandler<CreateProductsCommand, BatchResponse> commandHandler,
        CancellationToken ct
    ) =>
        commandHandler.HandleAsync(
            new CreateProductsCommand(new CreateProductsRequest([input])),
            ct
        );

    /// <summary>Deletes a product by ID; returns the same batch summary shape as the REST delete endpoint.</summary>
    [Authorize(Policy = Permission.Products.Delete)]
    public Task<BatchResponse> DeleteProduct(
        Guid id,
        [Service] ICommandHandler<DeleteProductsCommand, BatchResponse> handler,
        CancellationToken ct
    ) => handler.HandleAsync(new DeleteProductsCommand(new BatchDeleteRequest([id])), ct);
}
