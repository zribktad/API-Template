using APITemplate.Application.Common.Security;
using HotChocolate.Authorization;
using MediatR;

namespace APITemplate.Api.GraphQL.Mutations;

/// <summary>
/// Hot Chocolate mutation type that exposes product write operations (create and delete)
/// to the GraphQL schema, enforcing per-operation authorization policies.
/// </summary>
[Authorize]
public class ProductMutations
{
    /// <summary>Creates a new product via MediatR and returns the resulting product response.</summary>
    [Authorize(Policy = Permission.Products.Create)]
    public async Task<ProductResponse> CreateProduct(
        CreateProductRequest input,
        [Service] ISender sender,
        CancellationToken ct
    )
    {
        return await sender.Send(new CreateProductCommand(input), ct);
    }

    /// <summary>Deletes a product by its ID and returns <see langword="true"/> on success.</summary>
    [Authorize(Policy = Permission.Products.Delete)]
    public async Task<bool> DeleteProduct(Guid id, [Service] ISender sender, CancellationToken ct)
    {
        await sender.Send(new DeleteProductCommand(id), ct);
        return true;
    }
}
