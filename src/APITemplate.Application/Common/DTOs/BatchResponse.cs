namespace APITemplate.Application.Common.DTOs;

/// <summary>
/// Summarises the outcome of a batch operation, including per-item results and aggregate success/failure counts.
/// </summary>
public sealed record BatchResponse(
    IReadOnlyList<BatchResultItem> Results,
    int SuccessCount,
    int FailureCount
);

/// <summary>
/// Represents the outcome for one item within a batch operation, including its zero-based index,
/// success flag, created/affected ID, and any validation errors.
/// </summary>
public sealed record BatchResultItem(
    int Index,
    bool Success,
    Guid? Id,
    IReadOnlyList<string>? Errors
);
