using APITemplate.Application.Common.Validation;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Product.Validation;
using FluentValidation;

namespace APITemplate.Application.Features.Examples.Validation;

/// <summary>
/// FluentValidation validator for a single <see cref="BatchProductItem"/>; inherits data-annotation rules and adds the shared description-required-above-price-threshold rule.
/// </summary>
public sealed class BatchProductItemValidator : DataAnnotationsValidator<BatchProductItem>
{
    public BatchProductItemValidator()
    {
        RuleFor(x => x.Description).RequiredAbovePriceThreshold(x => x.Price);
    }
}
