using ErrorOr;

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
                code: ErrorCatalog.Auth.Forbidden,
                description: ErrorCatalog.Auth.ForbiddenOwnReviewsOnly
            );
    }

    public static class Reviews
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: ErrorCatalog.Reviews.ReviewNotFound,
                description: $"Review with id '{id}' not found."
            );

        public static Error ProductNotFoundForReview(Guid productId) =>
            Error.NotFound(
                code: ErrorCatalog.Reviews.ProductNotFoundForReview,
                description: $"Product with id '{productId}' not found."
            );
    }
}
