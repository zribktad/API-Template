using FluentValidation;

namespace APITemplate.Application.Common.CQRS;

/// <summary>
/// Reusable building blocks for batch command handlers — validation and existence checking.
/// </summary>
internal static class BatchHelper
{
    /// <summary>
    /// Builds the standard all-or-nothing failure response for a batch command.
    /// </summary>
    internal static BatchResponse ToAtomicFailureResponse(IReadOnlyList<BatchResultItem> failures)
    {
        return new BatchResponse(failures, 0, failures.Count);
    }

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
    /// Returns failures for items whose ID is not recognised by the <paramref name="exists"/> predicate.
    /// Items at indices in <paramref name="skip"/> are ignored.
    /// </summary>
    internal static List<BatchResultItem> MarkMissing<T>(
        IReadOnlyList<T> items,
        Func<Guid, bool> exists,
        string notFoundMessageTemplate,
        HashSet<int>? skip = null
    )
        where T : IHasId
    {
        var failures = new List<BatchResultItem>();

        for (var i = 0; i < items.Count; i++)
        {
            if (skip is not null && skip.Contains(i))
                continue;

            var id = items[i].Id;
            if (!exists(id))
                failures.Add(
                    new BatchResultItem(i, id, [string.Format(notFoundMessageTemplate, id)])
                );
        }

        return failures;
    }

    /// <inheritdoc cref="MarkMissing{T}(IReadOnlyList{T}, Func{Guid, bool}, string, HashSet{int}?)"/>
    internal static List<BatchResultItem> MarkMissing(
        IReadOnlyList<Guid> ids,
        Func<Guid, bool> exists,
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
            if (!exists(id))
                failures.Add(
                    new BatchResultItem(i, id, [string.Format(notFoundMessageTemplate, id)])
                );
        }

        return failures;
    }
}
