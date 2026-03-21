using FluentValidation;

namespace APITemplate.Application.Common.CQRS;

/// <summary>
/// Reusable building blocks for batch command handlers — validation and existence checking.
/// </summary>
internal static class BatchHelper
{
    /// <summary>
    /// Validates each item and returns a list of failures.
    /// </summary>
    internal static async Task<List<BatchResultItem>> ValidateAsync<T>(
        IValidator<T> validator,
        IReadOnlyList<T> items,
        Func<int, Guid?> idAt,
        CancellationToken ct
    )
    {
        var failures = new List<BatchResultItem>();

        for (var i = 0; i < items.Count; i++)
        {
            var validationResult = await validator.ValidateAsync(items[i], ct);

            if (!validationResult.IsValid)
            {
                failures.Add(
                    new BatchResultItem(
                        i,
                        idAt(i),
                        validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                    )
                );
            }
        }

        return failures;
    }

    /// <summary>
    /// Returns failures for items whose ID is not recognised by the <paramref name="exists"/> predicate.
    /// </summary>
    internal static List<BatchResultItem> MarkMissing<T>(
        IReadOnlyList<T> items,
        Func<T, Guid> idSelector,
        Func<Guid, bool> exists,
        string notFoundMessageTemplate
    )
    {
        var failures = new List<BatchResultItem>();

        for (var i = 0; i < items.Count; i++)
        {
            var id = idSelector(items[i]);
            if (!exists(id))
                failures.Add(
                    new BatchResultItem(i, id, [string.Format(notFoundMessageTemplate, id)])
                );
        }

        return failures;
    }
}
