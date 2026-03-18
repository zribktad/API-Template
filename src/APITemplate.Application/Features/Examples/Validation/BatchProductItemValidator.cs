using APITemplate.Application.Common.Validation;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Product.Validation;
using FluentValidation;

namespace APITemplate.Application.Features.Examples.Validation;

public sealed class BatchProductItemValidator : DataAnnotationsValidator<BatchProductItem>
{
    public BatchProductItemValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage(ProductValidationRules.DescriptionRequiredMessage)
            .When(x => x.Price > ProductValidationRules.DescriptionRequiredPriceThreshold);
    }
}
