using ErrorOr;
using ProductCatalog.Application.Common.Errors;
using ProductCatalog.Application.Features.Category.DTOs;
using ProductCatalog.Application.Features.Category.Specifications;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Application.Batch;
using SharedKernel.Application.Batch.Rules;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.DTOs;
using SharedKernel.Domain.Interfaces;
using Wolverine;
using CategoryEntity = ProductCatalog.Domain.Entities.Category;

namespace ProductCatalog.Application.Features.Category.Commands;

/// <summary>Updates multiple categories in a single batch operation.</summary>
public sealed record UpdateCategoriesCommand(UpdateCategoriesRequest Request);

/// <summary>Handles <see cref="UpdateCategoriesCommand"/> by validating all items, loading categories in bulk, and updating in a single transaction.</summary>
public sealed class UpdateCategoriesCommandHandler
{
    /// <summary>
    /// Wolverine compound-handler load step: validates and loads categories, short-circuiting the
    /// handler pipeline with a failure response when any validation rule fails.
    /// </summary>
    public static async Task<(
        HandlerContinuation,
        EntityLookup<CategoryEntity>?,
        OutgoingMessages
    )> LoadAsync(
        UpdateCategoriesCommand command,
        ICategoryRepository repository,
        FluentValidationBatchRule<UpdateCategoryItem> batchRule,
        CancellationToken ct
    )
    {
        IReadOnlyList<UpdateCategoryItem> items = command.Request.Items;
        BatchFailureContext<UpdateCategoryItem> context = new(items);

        await context.ApplyRulesAsync(ct, batchRule);

        HashSet<Guid> requestedIds = items
            .Where((_, i) => !context.IsFailed(i))
            .Select(item => item.Id)
            .ToHashSet();
        Dictionary<Guid, CategoryEntity> categoryMap = (
            await repository.ListAsync(new CategoriesByIdsSpecification(requestedIds), ct)
        ).ToDictionary(c => c.Id);

        await context.ApplyRulesAsync(
            ct,
            new MarkMissingByIdBatchRule<UpdateCategoryItem>(
                item => item.Id,
                categoryMap.Keys.ToHashSet(),
                ErrorCatalog.Categories.NotFoundMessage
            )
        );

        OutgoingMessages messages = new();

        if (context.HasFailures)
        {
            messages.RespondToSender(context.ToFailureResponse());
            return (HandlerContinuation.Stop, null, messages);
        }

        return (
            HandlerContinuation.Continue,
            new EntityLookup<CategoryEntity>(categoryMap),
            messages
        );
    }

    /// <summary>Applies changes in a single transaction.</summary>
    public static async Task<(ErrorOr<BatchResponse>, OutgoingMessages)> HandleAsync(
        UpdateCategoriesCommand command,
        EntityLookup<CategoryEntity> lookup,
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        IReadOnlyList<UpdateCategoryItem> items = command.Request.Items;
        IReadOnlyDictionary<Guid, CategoryEntity> categoryMap = lookup.Entities;

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                for (int i = 0; i < items.Count; i++)
                {
                    UpdateCategoryItem item = items[i];
                    CategoryEntity category = categoryMap[item.Id];

                    category.Name = item.Name;
                    category.Description = item.Description;

                    await repository.UpdateAsync(category, ct);
                }
            },
            ct
        );

        return (
            new BatchResponse([], items.Count, 0),
            CacheInvalidationCascades.ForTag(CacheTags.Categories)
        );
    }
}
