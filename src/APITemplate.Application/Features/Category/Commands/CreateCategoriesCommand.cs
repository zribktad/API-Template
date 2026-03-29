using APITemplate.Application.Common.Batch;
using APITemplate.Application.Common.Batch.Rules;
using ErrorOr;
using FluentValidation;
using SharedKernel.Application.Common.Events;
using Wolverine;
using CategoryEntity = APITemplate.Domain.Entities.Category;

namespace APITemplate.Application.Features.Category;

/// <summary>Creates multiple categories in a single batch operation.</summary>
public sealed record CreateCategoriesCommand(CreateCategoriesRequest Request);

/// <summary>Handles <see cref="CreateCategoriesCommand"/> by validating all items and persisting in a single transaction.</summary>
public sealed class CreateCategoriesCommandHandler
{
    public static async Task<(ErrorOr<BatchResponse>, OutgoingMessages)> HandleAsync(
        CreateCategoriesCommand command,
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateCategoryRequest> itemValidator,
        CancellationToken ct
    )
    {
        var items = command.Request.Items;
        var context = new BatchFailureContext<CreateCategoryRequest>(items);

        await context.ApplyRulesAsync(
            ct,
            new FluentValidationBatchRule<CreateCategoryRequest>(itemValidator)
        );

        if (context.HasFailures)
            return (context.ToFailureResponse(), CacheInvalidationCascades.None);

        var entities = items
            .Select(item => new CategoryEntity
            {
                Id = Guid.NewGuid(),
                Name = item.Name,
                Description = item.Description,
            })
            .ToList();

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.AddRangeAsync(entities, ct);
            },
            ct
        );

        return (
            new BatchResponse([], items.Count, 0),
            CacheInvalidationCascades.ForTag(CacheTags.Categories)
        );
    }
}
