using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Domain.Interfaces;
using FluentValidation;
using MediatR;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Examples.Handlers;

/// <summary>Initiates a batch product creation operation from the supplied request.</summary>
public sealed record BatchCreateProductsCommand(BatchCreateProductsRequest Request)
    : IRequest<BatchCreateProductsResponse>;

/// <summary>
/// Application-layer handler that validates each batch item individually and, when all items pass, persists all products in a single transaction.
/// Items that fail validation are reported in the response without aborting the entire batch unless any item is invalid.
/// </summary>
public sealed class BatchRequestHandlers
    : IRequestHandler<BatchCreateProductsCommand, BatchCreateProductsResponse>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<BatchProductItem> _itemValidator;

    public BatchRequestHandlers(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<BatchProductItem> itemValidator
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _itemValidator = itemValidator;
    }

    /// <summary>Validates every item, then either returns early with validation failures or persists all entities in one transaction and returns their IDs.</summary>
    public async Task<BatchCreateProductsResponse> Handle(
        BatchCreateProductsCommand command,
        CancellationToken ct
    )
    {
        var items = command.Request.Items;
        var results = new List<BatchResultItem>(items.Count);
        var successCount = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var validationResult = await _itemValidator.ValidateAsync(items[i], ct);
            if (!validationResult.IsValid)
            {
                results.Add(
                    new BatchResultItem(
                        i,
                        false,
                        null,
                        validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                    )
                );
            }
            else
            {
                results.Add(new BatchResultItem(i, true, null, null));
                successCount++;
            }
        }

        if (successCount < items.Count)
        {
            return new BatchCreateProductsResponse(
                results,
                successCount,
                items.Count - successCount
            );
        }

        var entities = new List<ProductEntity>(items.Count);
        var finalResults = new List<BatchResultItem>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var entity = new ProductEntity
            {
                Id = Guid.NewGuid(),
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
            };
            entities.Add(entity);
            finalResults.Add(new BatchResultItem(i, true, entity.Id, null));
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.AddRangeAsync(entities, ct);
            },
            ct
        );

        return new BatchCreateProductsResponse(finalResults, finalResults.Count, 0);
    }
}
