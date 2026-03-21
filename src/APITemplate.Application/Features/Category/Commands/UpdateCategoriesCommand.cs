using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Category.Specifications;
using FluentValidation;

namespace APITemplate.Application.Features.Category;

/// <summary>Updates multiple categories in a single batch operation.</summary>
public sealed record UpdateCategoriesCommand(UpdateCategoriesRequest Request)
    : ICommand<BatchResponse>;

/// <summary>Handles <see cref="UpdateCategoriesCommand"/> by validating all items, loading categories in bulk, and updating in a single transaction.</summary>
public sealed class UpdateCategoriesCommandHandler
    : ICommandHandler<UpdateCategoriesCommand, BatchResponse>
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IValidator<UpdateCategoryItem> _itemValidator;

    public UpdateCategoriesCommandHandler(
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IValidator<UpdateCategoryItem> itemValidator
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _itemValidator = itemValidator;
    }

    public async Task<BatchResponse> HandleAsync(
        UpdateCategoriesCommand command,
        CancellationToken ct
    )
    {
        var items = command.Request.Items;
        var results = new BatchResultItem[items.Count];
        var hasFailures = false;

        // Step 1: Validate each item individually
        for (var i = 0; i < items.Count; i++)
        {
            var validationResult = await _itemValidator.ValidateAsync(items[i], ct);
            if (!validationResult.IsValid)
            {
                results[i] = new BatchResultItem(
                    i,
                    false,
                    items[i].Id,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                );
                hasFailures = true;
            }
            else
            {
                results[i] = new BatchResultItem(i, true, items[i].Id, null);
            }
        }

        if (hasFailures)
        {
            var successCount = results.Count(r => r.Success);
            return new BatchResponse(results, successCount, results.Length - successCount);
        }

        // Step 2: Load all categories in a single query
        var ids = items.Select(item => item.Id).Distinct().ToList();
        var categories = await _repository.ListAsync(new CategoriesByIdsSpecification(ids), ct);
        var categoryMap = categories.ToDictionary(c => c.Id);

        for (var i = 0; i < items.Count; i++)
        {
            if (!categoryMap.ContainsKey(items[i].Id))
            {
                results[i] = new BatchResultItem(
                    i,
                    false,
                    items[i].Id,
                    [$"Category '{items[i].Id}' not found."]
                );
                hasFailures = true;
            }
        }

        if (hasFailures)
        {
            var successCount = results.Count(r => r.Success);
            return new BatchResponse(results, successCount, results.Length - successCount);
        }

        // Step 3: Update all in a single transaction
        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var category = categoryMap[item.Id];

                    category.Name = item.Name;
                    category.Description = item.Description;

                    await _repository.UpdateAsync(category, ct);
                }
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Categories), ct);

        return new BatchResponse(results, results.Length, 0);
    }
}
