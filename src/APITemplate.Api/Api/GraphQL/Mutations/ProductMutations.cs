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
        [Service] IQueryHandler<GetProductsQuery, ProductsResponse> queryHandler,
        CancellationToken ct
    )
    {
        var result = await commandHandler.HandleAsync(
            new CreateProductsCommand(new CreateProductsRequest([input])),
            ct
        );

        if (result.FailureCount > 0)
            throw new GraphQLException(string.Join("; ", result.Failures[0].Errors));

        var filter = new ProductFilter(
            Name: input.Name,
            Description: input.Description,
            MinPrice: input.Price,
            MaxPrice: input.Price,
            CategoryIds: input.CategoryId.HasValue ? [input.CategoryId.Value] : null,
            SortBy: ProductSortFields.CreatedAt.Value,
            SortDirection: "desc",
            PageNumber: 1,
            PageSize: 25
        );
        var products = await queryHandler.HandleAsync(new GetProductsQuery(filter), ct);
        var created = products.Page.Items.FirstOrDefault(item => IsSameCreatePayload(item, input));
        if (created is null)
            throw new GraphQLException("Created product could not be loaded.");

        return created;
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

    private static bool IsSameCreatePayload(ProductResponse item, CreateProductRequest input)
    {
        if (
            item.Name != input.Name
            || item.Description != input.Description
            || item.Price != input.Price
        )
            return false;

        if (item.CategoryId != input.CategoryId)
            return false;

        var expected = (input.ProductDataIds ?? []).Distinct().OrderBy(id => id);
        var actual = item.ProductDataIds.OrderBy(id => id);
        return expected.SequenceEqual(actual);
    }
}
