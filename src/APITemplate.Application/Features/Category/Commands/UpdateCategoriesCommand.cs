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
        var results = BatchHelper.Initialize(items.Count, i => items[i].Id);

        // Step 1: Validate each item (field-level rules — name, description, etc.)
        var failureCount = await BatchHelper.ValidateAsync(_itemValidator, items, results, ct);

        if (failureCount > 0)
            return new BatchResponse(results, results.Length - failureCount, failureCount);

        // Step 2: Load all target categories and mark missing ones as failed
        var categoryMap = (
            await _repository.ListAsync(
                new CategoriesByIdsSpecification(
                    items.Select(item => item.Id).Distinct().ToHashSet()
                ),
                ct
            )
        ).ToDictionary(c => c.Id);

        failureCount += BatchHelper.MarkMissing(
            results,
            new HashSet<Guid>(categoryMap.Keys),
            ErrorCatalog.Categories.NotFoundMessage
        );

        if (failureCount > 0)
            return new BatchResponse(results, results.Length - failureCount, failureCount);

        // Step 3: Apply changes in a single transaction
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
