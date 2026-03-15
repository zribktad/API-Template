namespace APITemplate.Application.Features.Product.DTOs;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    Guid? CategoryId,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<Guid> ProductDataIds
);
