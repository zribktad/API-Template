using System.ComponentModel.DataAnnotations;
using APITemplate.Application.Common.Validation;

namespace APITemplate.Application.Features.Examples.DTOs;

/// <summary>
/// Carries a list of product items to be created in a single batch operation; accepts between 1 and 100 items.
/// </summary>
public sealed record BatchCreateProductsRequest(
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    [MaxLength(100, ErrorMessage = "Maximum 100 items per batch.")]
        IReadOnlyList<BatchProductItem> Items
);

/// <summary>
/// Represents a single product within a batch create request.
/// </summary>
public sealed record BatchProductItem(
    [NotEmpty(ErrorMessage = "Product name is required.")] [MaxLength(200)] string Name,
    string? Description,
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive.")] decimal Price
);
