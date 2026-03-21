using APITemplate.Application.Common.CQRS;
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
        var results = new BatchResultItem[ids.Count];

        var distinctIds = ids.Distinct().ToHashSet();
        var categories = await _repository.ListAsync(
            new CategoriesByIdsSpecification(distinctIds),
            ct
        );
        var foundIds = categories.Select(c => c.Id).ToHashSet();

        for (var i = 0; i < ids.Count; i++)
            results[i] = new BatchResultItem(i, true, ids[i], null);

        var failureCount = BatchHelper.MarkMissing(
            results,
            ids.Count,
            i => ids[i],
            foundIds.Contains,
            ErrorCatalog.Categories.NotFoundMessage
        );

        if (failureCount > 0)
            return new BatchResponse(results, results.Length - failureCount, failureCount);

        await _unitOfWork.ExecuteInTransactionAsync(
            () => _repository.DeleteRangeAsync(categories, ct),
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Categories), ct);

        return new BatchResponse(results, results.Length, 0);
    }
}
