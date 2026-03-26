using ErrorOr;
using ProductCatalog.Application.Common.Errors;
using ProductCatalog.Application.Features.Category.Specifications;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Application.Batch;
using SharedKernel.Application.Batch.Rules;
using SharedKernel.Application.DTOs;
using SharedKernel.Domain.Interfaces;

namespace ProductCatalog.Application.Features.Category.Commands;

/// <summary>Soft-deletes multiple categories in a single batch operation.</summary>
public sealed record DeleteCategoriesCommand(BatchDeleteRequest Request);

/// <summary>Handles <see cref="DeleteCategoriesCommand"/> by loading all categories and deleting in a single transaction.</summary>
public sealed class DeleteCategoriesCommandHandler
{
    public static async Task<ErrorOr<BatchResponse>> HandleAsync(
        DeleteCategoriesCommand command,
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        IReadOnlyList<Guid> ids = command.Request.Ids;
        BatchFailureContext<Guid> context = new(ids);

        // Load all target categories and mark missing ones as failed
        List<Domain.Entities.Category> categories = await repository.ListAsync(
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
            return context.ToFailureResponse();

        // Remove categories in a single transaction
        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.DeleteRangeAsync(categories, ct);
            },
            ct
        );

        return new BatchResponse([], ids.Count, 0);
    }
}
