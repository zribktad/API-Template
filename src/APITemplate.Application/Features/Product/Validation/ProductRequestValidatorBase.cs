using APITemplate.Application.Common.Validation;
using FluentValidation;

namespace APITemplate.Application.Features.Product.Validation;

public static class ProductValidationRules
{
    public const decimal DescriptionRequiredPriceThreshold = 1000;
    public const string DescriptionRequiredMessage =
        "Description is required for products priced above 1000.";

    public static IRuleBuilderOptions<T, string?> RequiredAbovePriceThreshold<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        Func<T, decimal> priceAccessor
    ) =>
        ruleBuilder
            .NotEmpty()
            .WithMessage(DescriptionRequiredMessage)
            .When(x => priceAccessor(x) > DescriptionRequiredPriceThreshold);
}

public abstract class ProductRequestValidatorBase<T> : DataAnnotationsValidator<T>
    where T : class, IProductRequest
{
    protected ProductRequestValidatorBase()
    {
        RuleFor(x => x.Description).RequiredAbovePriceThreshold(x => x.Price);
    }
}
