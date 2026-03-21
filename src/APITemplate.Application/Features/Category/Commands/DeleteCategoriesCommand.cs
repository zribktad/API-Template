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

        var distinctIds = ids.Distinct().ToList();
        var categories = await _repository.ListAsync(
            new CategoriesByIdsSpecification(distinctIds),
            ct
        );
        var categoryMap = categories.ToDictionary(c => c.Id);

        var hasFailures = false;

        for (var i = 0; i < ids.Count; i++)
        {
            if (!categoryMap.ContainsKey(ids[i]))
            {
                results[i] = new BatchResultItem(
                    i,
                    false,
                    ids[i],
                    [$"Category '{ids[i]}' not found."]
                );
                hasFailures = true;
            }
            else
            {
                results[i] = new BatchResultItem(i, true, ids[i], null);
            }
        }

        if (hasFailures)
        {
            var successCount = results.Count(r => r.Success);
            return new BatchResponse(results, successCount, results.Length - successCount);
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                foreach (var category in categories)
                    await _repository.DeleteAsync(category, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Categories), ct);

        return new BatchResponse(results, results.Length, 0);
    }
}
