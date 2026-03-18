using APITemplate.Application.Common.Validation;
using FluentValidation;

namespace APITemplate.Application.Features.Product.Validation;

public static class ProductValidationRules
{
    public const decimal DescriptionRequiredPriceThreshold = 1000;
    public const string DescriptionRequiredMessage =
        "Description is required for products priced above 1000.";
}

public abstract class ProductRequestValidatorBase<T> : DataAnnotationsValidator<T>
    where T : class, IProductRequest
{
    protected ProductRequestValidatorBase()
    {
        // Cross-field rule: cannot be expressed via Data Annotations
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage(ProductValidationRules.DescriptionRequiredMessage)
            .When(x => x.Price > ProductValidationRules.DescriptionRequiredPriceThreshold);
    }
}
