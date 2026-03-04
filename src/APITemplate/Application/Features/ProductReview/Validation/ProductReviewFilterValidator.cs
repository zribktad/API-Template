using FluentValidation;

using APITemplate.Application.Features.Product.Validation;

namespace APITemplate.Application.Features.ProductReview.Validation;
public sealed class ProductReviewFilterValidator : AbstractValidator<ProductReviewFilter>
{
    public ProductReviewFilterValidator()
    {
        Include(new PaginationFilterValidator());

        RuleFor(x => x.MinRating)
            .InclusiveBetween(1, 5).WithMessage("MinRating must be between 1 and 5.")
            .When(x => x.MinRating.HasValue);

        RuleFor(x => x.MaxRating)
            .InclusiveBetween(1, 5).WithMessage("MaxRating must be between 1 and 5.")
            .When(x => x.MaxRating.HasValue);

        RuleFor(x => x.MaxRating)
            .GreaterThanOrEqualTo(x => x.MinRating!.Value).WithMessage("MaxRating must be greater than or equal to MinRating.")
            .When(x => x.MinRating.HasValue && x.MaxRating.HasValue);

        RuleFor(x => x.CreatedTo)
            .GreaterThanOrEqualTo(x => x.CreatedFrom!.Value).WithMessage("CreatedTo must be greater than or equal to CreatedFrom.")
            .When(x => x.CreatedFrom.HasValue && x.CreatedTo.HasValue);

        RuleFor(x => x.SortBy)
            .Must(s => s is null
                || s.Equals("createdAt", StringComparison.OrdinalIgnoreCase)
                || s.Equals("rating", StringComparison.OrdinalIgnoreCase)
                || s.Equals("reviewerName", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortBy must be one of: createdAt, rating, reviewerName.");

        RuleFor(x => x.SortDirection)
            .Must(s => s is null || s.Equals("asc", StringComparison.OrdinalIgnoreCase) || s.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be one of: asc, desc.");
    }
}
