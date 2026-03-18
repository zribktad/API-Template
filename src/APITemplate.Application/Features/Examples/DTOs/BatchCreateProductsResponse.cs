namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record BatchCreateProductsResponse(
    IReadOnlyList<BatchResultItem> Results,
    int SuccessCount,
    int FailureCount
);

public sealed record BatchResultItem(
    int Index,
    bool Success,
    Guid? Id,
    IReadOnlyList<string>? Errors
);
