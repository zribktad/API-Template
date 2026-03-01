namespace APITemplate.Application.DTOs;

public sealed record ProductFilter(
    string? Name,
    string? Description,
    decimal? MinPrice,
    decimal? MaxPrice,
    DateTime? CreatedFrom,
    DateTime? CreatedTo);
