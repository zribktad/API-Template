using FluentValidation;

namespace APITemplate.Application.Common.CQRS;

/// <summary>
/// Encapsulates batch failure tracking — maintains both the failure list and the set of
/// failed indices, eliminating repetitive manual bookkeeping in batch command handlers.
/// Delegates to <see cref="BatchFailureCollectorHelper"/> static methods to avoid logic duplication.
/// </summary>
internal sealed class BatchFailureCollector<T>
    where T : IHasId
{
    private readonly IReadOnlyList<T> _items;
    private readonly List<BatchResultItem> _failures = [];
    private readonly HashSet<int> _failedIndices = [];

    internal BatchFailureCollector(IReadOnlyList<T> items) => _items = items;

    internal IReadOnlyList<T> Items => _items;
    internal bool HasFailures => _failures.Count > 0;
    internal HashSet<int> FailedIndices => _failedIndices;

    /// <summary>
    /// Validates each item and records failures. Uses <see cref="IHasId.Id"/> for the failure identity.
    /// </summary>
    internal async Task ValidateAsync(IValidator<T> validator, CancellationToken ct)
    {
        AddFailures(
            await BatchFailureCollectorHelper.ValidateAsync(
                validator,
                _items,
                i => _items[i].Id,
                ct
            )
        );
    }

    /// <summary>
    /// Marks items whose ID is not present in <paramref name="foundIds"/>.
    /// Already-failed indices are skipped automatically.
    /// </summary>
    internal void MarkMissing(IReadOnlySet<Guid> foundIds, string notFoundMessageTemplate)
    {
        AddFailures(
            BatchFailureCollectorHelper.MarkMissing(
                _items.Select(x => x.Id).ToList(),
                foundIds,
                notFoundMessageTemplate,
                _failedIndices
            )
        );
    }

    /// <summary>
    /// Absorbs externally produced failures (e.g. from <c>ProductValidationHelper</c>)
    /// and updates the failed-indices set accordingly.
    /// </summary>
    internal void AddFailures(IEnumerable<BatchResultItem> failures)
    {
        foreach (var failure in failures)
        {
            _failures.Add(failure);
            _failedIndices.Add(failure.Index);
        }
    }

    internal BatchResponse ToFailureResponse() => new(_failures, 0, _failures.Count);
}
