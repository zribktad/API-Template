namespace APITemplate.Api.GraphQL.Models;

public sealed record ProductReviewPageResult(
    IEnumerable<ProductReviewResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
