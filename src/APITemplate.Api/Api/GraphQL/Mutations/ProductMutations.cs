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
        [Service] ICommandHandler<CreateProductsCommand, BatchResponse> commandHandler,
        [Service] IQueryHandler<GetProductByIdQuery, ProductResponse?> queryHandler,
        CancellationToken ct
    )
    {
        var productId = Guid.NewGuid();
        var result = await commandHandler.HandleAsync(
            new CreateProductsCommand(new CreateProductsRequest([input with { Id = productId }])),
            ct
        );

        if (result.FailureCount > 0)
            throw new GraphQLException(string.Join("; ", result.Failures[0].Errors));

        return (await queryHandler.HandleAsync(new GetProductByIdQuery(productId), ct))!;
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
            throw new GraphQLException(string.Join("; ", result.Failures[0].Errors));

        return true;
    }
}
