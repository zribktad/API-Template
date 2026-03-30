using FluentValidation;
using FluentValidation.Results;
using SharedKernel.Domain.Entities.Contracts;

namespace SharedKernel.Application.Batch.Rules;

public sealed class FluentValidationBatchRule<TItem>(
    IValidator<TItem> validator,
    IValidationMetrics metrics
) : IBatchRule<TItem>
{
    public async Task ApplyAsync(BatchFailureContext<TItem> context, CancellationToken ct)
    {
        for (int i = 0; i < context.Items.Count; i++)
        {
            if (context.IsFailed(i))
                continue;

            ValidationResult validationResult = await validator.ValidateAsync(context.Items[i], ct);
            if (!validationResult.IsValid)
            {
                Guid? id = context.Items[i] is IHasId hasId ? hasId.Id : null;
                context.AddFailure(
                    i,
                    id,
                    validationResult.Errors.Select(error => error.ErrorMessage).ToList()
                );

                metrics.RecordFailure(typeof(TItem).Name, typeof(TItem), validationResult.Errors);
            }
        }
    }
}
