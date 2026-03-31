using ErrorOr;
using SharedDomainErrors = SharedKernel.Application.Errors.DomainErrors;
using SharedErrorCatalog = SharedKernel.Application.Errors.ErrorCatalog;

namespace Reviews.Application.Common.Errors;

/// <summary>
/// Factory methods producing <see cref="Error"/> instances for Reviews-specific error codes.
/// </summary>
public static class DomainErrors
{
    public static class Auth
    {
        public static Error ForbiddenOwnReviewsOnly() =>
            Error.Forbidden(
                code: SharedErrorCatalog.Auth.Forbidden,
                description: ErrorCatalog.Auth.ForbiddenOwnReviewsOnly
            );
    }

    public static class Reviews
    {
        public static Error NotFound(Guid id) =>
            SharedDomainErrors.General.NotFound(ErrorCatalog.Reviews.ReviewNotFound, "Review", id);

        public static Error ProductNotFoundForReview(Guid productId) =>
            SharedDomainErrors.General.NotFound(
                ErrorCatalog.Reviews.ProductNotFoundForReview,
                "Product",
                productId
            );
    }
}
