using APITemplate.Application.Common.Batch;
using APITemplate.Application.Common.Events;
using APITemplate.Domain.Entities;
using ErrorOr;
using FluentValidation;
using Wolverine;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product;

/// <summary>Updates products and syncs their product-data links in a single batch.</summary>
public sealed record UpdateProductsCommand(UpdateProductsRequest Request);

public sealed class UpdateProductsCommandHandler
{
    public static async Task<(
        HandlerContinuation,
        EntityLookup<ProductEntity>?,
        OutgoingMessages
    )> LoadAsync(
        UpdateProductsCommand command,
        IProductRepository repository,
        ICategoryRepository categoryRepository,
        IProductDataRepository productDataRepository,
        IValidator<UpdateProductItem> itemValidator,
        CancellationToken ct
    )
    {
        (BatchResponse? failure, Dictionary<Guid, ProductEntity>? productMap) =
            await UpdateProductsValidator.ValidateAndLoadAsync(
                command,
                repository,
                categoryRepository,
                productDataRepository,
                itemValidator,
                ct
            );

        OutgoingMessages messages = new();

        if (failure is not null)
        {
            messages.RespondToSender(failure);
            return (HandlerContinuation.Stop, null, messages);
        }

        return (
            HandlerContinuation.Continue,
            new EntityLookup<ProductEntity>(productMap!),
            messages
        );
    }

    public static async Task<(ErrorOr<BatchResponse>, OutgoingMessages)> HandleAsync(
        UpdateProductsCommand command,
        EntityLookup<ProductEntity> lookup,
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        IReadOnlyList<UpdateProductItem> items = command.Request.Items;
        IReadOnlyDictionary<Guid, ProductEntity> productMap = lookup.Entities;

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                for (int i = 0; i < items.Count; i++)
                {
                    UpdateProductItem item = items[i];
                    ProductEntity product = productMap[item.Id];

                    product.UpdateDetails(item.Name, item.Description, item.Price, item.CategoryId);

                    if (item.ProductDataIds is not null)
                    {
                        HashSet<Guid> targetIds = item.ProductDataIds.ToHashSet();
                        Dictionary<Guid, ProductDataLink> existingById =
                            product.ProductDataLinks.ToDictionary(link => link.ProductDataId);
                        product.SyncProductDataLinks(targetIds, existingById);
                    }

                    await repository.UpdateAsync(product, ct);
                }
            },
            ct
        );

        return (
            new BatchResponse([], items.Count, 0),
            [new CacheInvalidationNotification(CacheTags.Products)]
        );
    }
}
