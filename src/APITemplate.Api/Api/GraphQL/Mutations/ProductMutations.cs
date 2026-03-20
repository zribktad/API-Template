using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Security;
using HotChocolate.Authorization;

namespace APITemplate.Api.GraphQL.Mutations;

/// <summary>
/// Hot Chocolate mutation type that exposes product write operations (create and delete)
/// to the GraphQL schema, enforcing per-operation authorization policies.
/// </summary>
[Authorize]
public class ProductMutations
{
    /// <summary>Creates a new product and returns the resulting product response.</summary>
    [Authorize(Policy = Permission.Products.Create)]
    public async Task<ProductResponse> CreateProduct(
        CreateProductRequest input,
        [Service] ICommandHandler<CreateProductCommand, ProductResponse> handler,
        CancellationToken ct
    )
    {
        return await handler.HandleAsync(new CreateProductCommand(input), ct);
    }

    /// <summary>Deletes a product by its ID and returns <see langword="true"/> on success.</summary>
    [Authorize(Policy = Permission.Products.Delete)]
    public async Task<bool> DeleteProduct(
        Guid id,
        [Service] ICommandHandler<DeleteProductCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new DeleteProductCommand(id), ct);
        return true;
    }
}
