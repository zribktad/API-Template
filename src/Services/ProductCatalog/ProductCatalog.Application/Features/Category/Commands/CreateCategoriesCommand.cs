using ErrorOr;
using FluentValidation;
using ProductCatalog.Application.Features.Category.DTOs;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Application.Batch;
using SharedKernel.Application.Batch.Rules;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.DTOs;
using SharedKernel.Domain.Interfaces;
using Wolverine;
using CategoryEntity = ProductCatalog.Domain.Entities.Category;

namespace ProductCatalog.Application.Features.Category.Commands;

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
        IReadOnlyList<CreateCategoryRequest> items = command.Request.Items;
        BatchFailureContext<CreateCategoryRequest> context = new(items);

        await context.ApplyRulesAsync(
            ct,
            new FluentValidationBatchRule<CreateCategoryRequest>(itemValidator)
        );

        if (context.HasFailures)
            return (context.ToFailureResponse(), CacheInvalidationCascades.None);

        List<CategoryEntity> entities = items
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
