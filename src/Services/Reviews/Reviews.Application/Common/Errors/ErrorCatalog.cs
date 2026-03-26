namespace Reviews.Application.Common.Errors;

/// <summary>
/// Structured error codes for the Reviews microservice.
/// </summary>
public static class ErrorCatalog
{
    /// <summary>Error codes for authentication and authorisation failures.</summary>
    public static class Auth
    {
        public const string Forbidden = "AUTH-0403";
        public const string ForbiddenOwnReviewsOnly = "You can only delete your own reviews.";
    }

    /// <summary>Error codes specific to the Reviews domain.</summary>
    public static class Reviews
    {
        public const string ProductNotFoundForReview = "REV-2101";
        public const string ReviewNotFound = "REV-0404";
    }
}
