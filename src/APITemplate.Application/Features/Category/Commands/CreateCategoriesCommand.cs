using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
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
        var results = new BatchResultItem[items.Count];
        var failureCount = await BatchHelper.ValidateAsync(
            _itemValidator,
            items,
            results,
            _ => null,
            ct
        );

        if (failureCount > 0)
            return new BatchResponse(results, results.Length - failureCount, failureCount);

        var entities = new List<CategoryEntity>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var entity = new CategoryEntity
            {
                Id = Guid.NewGuid(),
                Name = item.Name,
                Description = item.Description,
            };
            entities.Add(entity);
            results[i] = new BatchResultItem(i, true, entity.Id, null);
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.AddRangeAsync(entities, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Categories), ct);
        return new BatchResponse(results, results.Length, 0);
    }
}
