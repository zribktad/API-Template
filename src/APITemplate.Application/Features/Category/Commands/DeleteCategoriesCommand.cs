using APITemplate.Application.Common.Batch;
using APITemplate.Application.Common.Batch.Rules;
using APITemplate.Application.Features.Category.Specifications;
using ErrorOr;
using SharedKernel.Application.Common.Events;
using Wolverine;

namespace APITemplate.Application.Features.Category;

/// <summary>Soft-deletes multiple categories in a single batch operation.</summary>
public sealed record DeleteCategoriesCommand(BatchDeleteRequest Request);

/// <summary>Handles <see cref="DeleteCategoriesCommand"/> by loading all categories and deleting in a single transaction.</summary>
public sealed class DeleteCategoriesCommandHandler
{
    public static async Task<(ErrorOr<BatchResponse>, OutgoingMessages)> HandleAsync(
        DeleteCategoriesCommand command,
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
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

        if (context.HasFailures)
            return (context.ToFailureResponse(), CacheInvalidationCascades.None);

        // Remove categories in a single transaction
        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.DeleteRangeAsync(categories, ct);
            },
            ct
        );

        return (
            new BatchResponse([], ids.Count, 0),
            CacheInvalidationCascades.ForTag(CacheTags.Categories)
        );
    }
}
