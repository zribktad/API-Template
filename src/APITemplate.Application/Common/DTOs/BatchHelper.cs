using FluentValidation;

namespace APITemplate.Application.Common.DTOs;

/// <summary>
/// Reusable building blocks for batch command handlers — initialization, validation and existence checking.
/// </summary>
internal static class BatchHelper
{
    /// <summary>
    /// Creates a results array with all items marked as successful.
    /// </summary>
    internal static BatchResultItem[] Initialize(int count, Func<int, Guid?> idAt)
    {
        var results = new BatchResultItem[count];

        for (var i = 0; i < count; i++)
            results[i] = new BatchResultItem(i, true, idAt(i), null);

        return results;
    }

    /// <summary>
    /// Validates each item and marks failures in the pre-initialized results array.
    /// Returns the number of failures.
    /// </summary>
    internal static async Task<int> ValidateAsync<T>(
        IValidator<T> validator,
        IReadOnlyList<T> items,
        BatchResultItem[] results,
        CancellationToken ct
    )
    {
        var failureCount = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var validationResult = await validator.ValidateAsync(items[i], ct);

            if (!validationResult.IsValid)
            {
                results[i] = new BatchResultItem(
                    i,
                    false,
                    results[i].Id,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                );
                failureCount++;
            }
        }

        return failureCount;
    }

    /// <summary>
    /// Marks items as failed when their ID is not found among loaded entities.
    /// Items that already failed a previous step are skipped.
    /// Returns the number of newly marked failures.
    /// </summary>
    internal static int MarkMissing(
        BatchResultItem[] results,
        HashSet<Guid> foundIds,
        string notFoundMessageTemplate
    )
    {
        var newFailures = 0;

        for (var i = 0; i < results.Length; i++)
        {
            if (!results[i].Success)
                continue;

            var id = results[i].Id!.Value;
            if (foundIds.Contains(id))
                continue;

            results[i] = new BatchResultItem(
                i,
                false,
                id,
                [string.Format(notFoundMessageTemplate, id)]
            );
            newFailures++;
        }

        return newFailures;
    }
}
