namespace APITemplate.Application.DTOs;

public interface IProductRequest
{
    string Name { get; }
    string? Description { get; }
    decimal Price { get; }
}
