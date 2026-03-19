using System.ComponentModel.DataAnnotations;
using APITemplate.Application.Common.Validation;

namespace APITemplate.Application.Features.Examples.DTOs;

// Mutable class (not record) — required because JsonPatch mutates in-place
public sealed class PatchableProductDto
{
    [NotEmpty(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive.")]
    public decimal Price { get; set; }

    public Guid? CategoryId { get; set; }
}
