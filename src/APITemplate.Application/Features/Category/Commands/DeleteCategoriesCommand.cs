using APITemplate.Application.Common.Batch;
using APITemplate.Application.Common.Batch.Rules;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Category.Specifications;
using ErrorOr;
using Wolverine;
using CategoryEntity = APITemplate.Domain.Entities.Category;

namespace APITemplate.Application.Features.Category;

/// <summary>Soft-deletes multiple categories in a single batch operation.</summary>
public sealed record DeleteCategoriesCommand(BatchDeleteRequest Request);

/// <summary>Handles <see cref="DeleteCategoriesCommand"/> by loading all categories and deleting in a single transaction.</summary>
public sealed class DeleteCategoriesCommandHandler
{
    /// <summary>
    /// Wolverine compound-handler load step: loads categories and marks missing ones,
    /// short-circuiting the handler pipeline with a failure response when any ID is not found.
    /// </summary>
    public static async Task<(
        HandlerContinuation,
        List<CategoryEntity>?,
        OutgoingMessages
    )> LoadAsync(
        DeleteCategoriesCommand command,
        ICategoryRepository repository,
        CancellationToken ct
    )
    {
        var ids = command.Request.Ids;
        var context = new BatchFailureContext<Guid>(ids);

        // Load all target categories and mark missing ones as failed
        var categories = await repository.ListAsync(
            new CategoriesByIdsSpecification(ids.ToHashSet()),
            ct
        );

        await context.ApplyRulesAsync(
            ct,
            new MarkMissingByIdBatchRule<Guid>(
                id => id,
                categories.Select(category => category.Id).ToHashSet(),
                ErrorCatalog.Categories.NotFoundMessage
            )
        );

        OutgoingMessages messages = new();

        if (context.HasFailures)
        {
            messages.RespondToSender(context.ToFailureResponse());
            return (HandlerContinuation.Stop, null, messages);
        }

        return (HandlerContinuation.Continue, categories, messages);
    }

    /// <summary>Removes categories in a single transaction.</summary>
    public static async Task<(ErrorOr<BatchResponse>, OutgoingMessages)> HandleAsync(
        DeleteCategoriesCommand command,
        List<CategoryEntity> categories,
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        var ids = command.Request.Ids;

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.DeleteRangeAsync(categories, ct);
            },
            ct
        );

        return (
            new BatchResponse([], ids.Count, 0),
            [new CacheInvalidationNotification(CacheTags.Categories)]
        );
    }
}
