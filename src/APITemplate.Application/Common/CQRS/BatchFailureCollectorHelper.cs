using FluentValidation;

namespace APITemplate.Application.Common.CQRS;

/// <summary>
/// Static batch helpers for command handlers that don't use the
/// <see cref="BatchFailureCollector{T}"/> (e.g. create and delete handlers).
/// </summary>
internal static class BatchFailureCollectorHelper
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
    /// Returns failures for items that provide an explicit ID appearing more than once in the request.
    /// Items at indices in <paramref name="skip"/> are ignored.
    /// </summary>
    internal static List<BatchResultItem> MarkDuplicateOptionalIds<T>(
        IReadOnlyList<T> items,
        Func<T, Guid?> idSelector,
        string duplicateMessageTemplate,
        HashSet<int>? skip = null
    )
    {
        var duplicateIds = items
            .Select((item, index) => new { Index = index, Id = idSelector(item) })
            .Where(x => x.Id.HasValue && (skip is null || !skip.Contains(x.Index)))
            .GroupBy(x => x.Id!.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        if (duplicateIds.Count == 0)
            return [];

        var failures = new List<BatchResultItem>();

        for (var i = 0; i < items.Count; i++)
        {
            if (skip is not null && skip.Contains(i))
                continue;

            var id = idSelector(items[i]);
            if (id.HasValue && duplicateIds.Contains(id.Value))
            {
                failures.Add(
                    new BatchResultItem(i, id, [string.Format(duplicateMessageTemplate, id.Value)])
                );
            }
        }

        return failures;
    }

    /// <summary>
    /// Returns failures for items that provide an explicit ID already present in storage.
    /// Items at indices in <paramref name="skip"/> are ignored.
    /// </summary>
    internal static List<BatchResultItem> MarkExistingOptionalIds<T>(
        IReadOnlyList<T> items,
        Func<T, Guid?> idSelector,
        IReadOnlySet<Guid> existingIds,
        string alreadyExistsMessageTemplate,
        HashSet<int>? skip = null
    )
    {
        var failures = new List<BatchResultItem>();

        for (var i = 0; i < items.Count; i++)
        {
            if (skip is not null && skip.Contains(i))
                continue;

            var id = idSelector(items[i]);
            if (id.HasValue && existingIds.Contains(id.Value))
            {
                failures.Add(
                    new BatchResultItem(
                        i,
                        id,
                        [string.Format(alreadyExistsMessageTemplate, id.Value)]
                    )
                );
            }
        }

        return failures;
    }

    /// <summary>
    /// Returns failures for IDs not present in <paramref name="foundIds"/>.
    /// Items at indices in <paramref name="skip"/> are ignored.
    /// </summary>
    internal static List<BatchResultItem> MarkMissing(
        IReadOnlyList<Guid> ids,
        IReadOnlySet<Guid> foundIds,
        string notFoundMessageTemplate,
        HashSet<int>? skip = null
    )
    {
        var failures = new List<BatchResultItem>();

        for (var i = 0; i < ids.Count; i++)
        {
            if (skip is not null && skip.Contains(i))
                continue;

            var id = ids[i];
            if (!foundIds.Contains(id))
                failures.Add(
                    new BatchResultItem(i, id, [string.Format(notFoundMessageTemplate, id)])
                );
        }

        return failures;
    }
}
