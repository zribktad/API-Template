using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Domain.Interfaces;
using FluentValidation;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Examples;

public sealed record BatchCreateProductsCommand(BatchCreateProductsRequest Request)
    : ICommand<BatchCreateProductsResponse>;

public sealed class BatchCreateProductsCommandHandler
    : ICommandHandler<BatchCreateProductsCommand, BatchCreateProductsResponse>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<BatchProductItem> _itemValidator;

    public BatchCreateProductsCommandHandler(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<BatchProductItem> itemValidator
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _itemValidator = itemValidator;
    }

    public async Task<BatchCreateProductsResponse> HandleAsync(
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
