using FluentValidation;
using ProductCatalog.Application.Features.Category.DTOs;
using SharedKernel.Application.Validation;

namespace ProductCatalog.Application.Features.Category.Validation;

/// <summary>
/// FluentValidation validator for <see cref="CategoryFilter"/>.
/// </summary>
public sealed class CategoryFilterValidator : AbstractValidator<CategoryFilter>
{
    public CategoryFilterValidator()
    {
        Include(new PaginationFilterValidator());
        Include(new SortableFilterValidator<CategoryFilter>(CategorySortFields.Map.AllowedNames));
    }
}
