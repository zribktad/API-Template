using FluentValidation;

namespace APITemplate.Application.Common.DTOs;

/// <summary>
/// Reusable building blocks for batch command handlers — validation and existence checking.
/// </summary>
internal static class BatchHelper
{
    /// <summary>
    /// Validates each item using FluentValidation and populates the results array.
    /// Returns the number of failures.
    /// </summary>
    internal static async Task<int> ValidateAsync<T>(
        IValidator<T> validator,
        IReadOnlyList<T> items,
        BatchResultItem[] results,
        Func<int, Guid?> idAt,
        CancellationToken ct
    )
    {
        var failureCount = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var id = idAt(i);
            var validationResult = await validator.ValidateAsync(items[i], ct);

            if (validationResult.IsValid)
            {
                results[i] = new BatchResultItem(i, true, id, null);
            }
            else
            {
                results[i] = new BatchResultItem(
                    i,
                    false,
                    id,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                );
                failureCount++;
            }
        }

        return failureCount;
    }

    /// <summary>
    /// Marks items as failed when their ID is not found among loaded entities.
    /// Returns the number of newly marked failures.
    /// </summary>
    internal static int MarkMissing(
        BatchResultItem[] results,
        int count,
        Func<int, Guid> idAt,
        Func<Guid, bool> exists,
        string notFoundMessageTemplate
    )
    {
        var failureCount = 0;

        for (var i = 0; i < count; i++)
        {
            var id = idAt(i);
            if (exists(id))
                continue;

            results[i] = new BatchResultItem(
                i,
                false,
                id,
                [string.Format(notFoundMessageTemplate, id)]
            );
            failureCount++;
        }

        return failureCount;
    }
}
