using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.CQRS.Rules;
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
        var context = new BatchFailureContext<CreateCategoryRequest>(items);

        await context.ApplyRulesAsync(
            ct,
            new FluentValidationBatchRule<CreateCategoryRequest>(_itemValidator)
        );

        if (context.HasFailures)
            return context.ToFailureResponse();

        var entities = items
            .Select(item => new CategoryEntity
            {
                Id = Guid.NewGuid(),
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
