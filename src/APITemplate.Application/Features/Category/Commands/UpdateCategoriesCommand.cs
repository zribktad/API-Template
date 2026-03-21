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
        var collector = new BatchFailureCollector<UpdateCategoryItem>(items);

        // Step 1: Validate each item (field-level rules — name, description, etc.)
        await collector.ValidateAsync(_itemValidator, ct);

        // Step 2: Load all target categories and mark missing ones as failed
        var categoryMap = (
            await _repository.ListAsync(
                new CategoriesByIdsSpecification(items.Select(item => item.Id).ToHashSet()),
                ct
            )
        ).ToDictionary(c => c.Id);

        collector.MarkMissing(
            categoryMap.Keys.ToHashSet(),
            ErrorCatalog.Categories.NotFoundMessage
        );

        if (collector.HasFailures)
            return collector.ToFailureResponse();

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

        return new BatchResponse([], items.Count, 0);
    }
}
