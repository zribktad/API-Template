namespace APITemplate.Api.GraphQL.Models;

public sealed record ProductPageResult(
    IEnumerable<ProductResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
