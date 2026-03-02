namespace APITemplate.Application.DTOs;

public sealed record ProductCategoryStatsResponse(
    Guid CategoryId,
    string CategoryName,
    long ProductCount,
    decimal AveragePrice,
    long TotalReviews);
