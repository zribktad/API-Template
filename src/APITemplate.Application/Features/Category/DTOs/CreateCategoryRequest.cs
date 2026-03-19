namespace APITemplate.Application.Features.Category.DTOs;

/// <summary>
/// Payload for creating a new category, carrying the name and optional description.
/// </summary>
public sealed record CreateCategoryRequest(string Name, string? Description);
