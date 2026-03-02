namespace APITemplate.Application.DTOs;

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description);
