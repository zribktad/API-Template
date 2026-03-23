using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.CQRS.Rules;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Category.Specifications;

namespace APITemplate.Application.Features.Category;

/// <summary>Soft-deletes multiple categories in a single batch operation.</summary>
public sealed record DeleteCategoriesCommand(BatchDeleteRequest Request) : ICommand<BatchResponse>;

/// <summary>Handles <see cref="DeleteCategoriesCommand"/> by loading all categories and deleting in a single transaction.</summary>
public sealed class DeleteCategoriesCommandHandler
    : ICommandHandler<DeleteCategoriesCommand, BatchResponse>
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public DeleteCategoriesCommandHandler(
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<BatchResponse> HandleAsync(
        DeleteCategoriesCommand command,
        CancellationToken ct
    )
    {
        var ids = command.Request.Ids;
        var context = new BatchFailureContext<Guid>(ids);

        // Step 1: Load all target categories and mark missing ones as failed
        var categories = await _repository.ListAsync(
            new CategoriesByIdsSpecification(ids.ToHashSet()),
            ct
        );

        await context.ApplyRulesAsync(
            ct,
            new MarkMissingIdsBatchRule(
                categories.Select(category => category.Id).ToHashSet(),
                ErrorCatalog.Categories.NotFoundMessage
            )
        );

        if (context.HasFailures)
            return context.ToFailureResponse();

        // Step 2: Remove categories in a single transaction
        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.DeleteRangeAsync(categories, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Categories), ct);

        return new BatchResponse([], ids.Count, 0);
    }
}
