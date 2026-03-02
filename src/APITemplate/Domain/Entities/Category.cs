namespace APITemplate.Domain.Entities;

public sealed class Category
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
