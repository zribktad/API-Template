using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Category.Specifications;
using FluentValidation;
using CategoryEntity = APITemplate.Domain.Entities.Category;

namespace APITemplate.Application.Features.Category;

/// <summary>Creates multiple categories in a single batch operation.</summary>
public sealed record CreateCategoriesCommand(CreateCategoriesRequest Request)
    : ICommand<BatchResponse>;

/// <summary>Handles <see cref="CreateCategoriesCommand"/> by validating all items and persisting in a single transaction.</summary>
public sealed class CreateCategoriesCommandHandler
    : ICommandHandler<CreateCategoriesCommand, BatchResponse>
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IValidator<CreateCategoryRequest> _itemValidator;

    public CreateCategoriesCommandHandler(
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IValidator<CreateCategoryRequest> itemValidator
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _itemValidator = itemValidator;
    }

    public async Task<BatchResponse> HandleAsync(
        CreateCategoriesCommand command,
        CancellationToken ct
    )
    {
        var items = command.Request.Items;
        var failures = await BatchHelper.ValidateAsync(_itemValidator, items, _ => null, ct);
        var failedIndices = failures.Select(f => f.Index).ToHashSet();

        var duplicateIdFailures = BatchHelper.MarkDuplicateOptionalIds(
            items,
            item => item.Id,
            ErrorCatalog.Categories.DuplicateIdMessage,
            failedIndices
        );
        failures.AddRange(duplicateIdFailures);
        failedIndices.UnionWith(duplicateIdFailures.Select(f => f.Index));

        var explicitIds = items
            .Select((item, index) => new { Item = item, Index = index })
            .Where(x => !failedIndices.Contains(x.Index) && x.Item.Id.HasValue)
            .Select(x => x.Item.Id!.Value)
            .ToHashSet();

        if (explicitIds.Count > 0)
        {
            var existingIds = (
                await _repository.ListAsync(
                    new CategoriesByIdsSpecification(explicitIds, includeDeleted: true),
                    ct
                )
            )
                .Select(category => category.Id)
                .ToHashSet();

            var existingIdFailures = BatchHelper.MarkExistingOptionalIds(
                items,
                item => item.Id,
                existingIds,
                ErrorCatalog.Categories.AlreadyExistsMessage,
                failedIndices
            );
            failures.AddRange(existingIdFailures);
            failedIndices.UnionWith(existingIdFailures.Select(f => f.Index));
        }

        if (failures.Count > 0)
            return BatchHelper.ToAtomicFailureResponse(failures);

        var entities = items
            .Select(item => new CategoryEntity
            {
                Id = item.Id ?? Guid.NewGuid(),
                Name = item.Name,
                Description = item.Description,
            })
            .ToList();

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.AddRangeAsync(entities, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Categories), ct);
        return new BatchResponse([], items.Count, 0);
    }
}
