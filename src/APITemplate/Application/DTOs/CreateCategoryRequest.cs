namespace APITemplate.Application.DTOs;

public sealed record CreateCategoryRequest(
    string Name,
    string? Description);
