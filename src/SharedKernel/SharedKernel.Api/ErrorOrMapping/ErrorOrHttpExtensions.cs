using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SharedKernel.Api.ErrorOrMapping;

/// <summary>
/// Maps <see cref="ErrorOr{T}"/> to minimal / Wolverine HTTP <see cref="IResult"/> using the same ProblemDetails shape as MVC helpers.
/// </summary>
public static class ErrorOrHttpExtensions
{
    /// <summary>Maps a list of <see cref="ErrorOr.Error"/> entries to an RFC 7807 JSON response.</summary>
    public static IResult ToProblemDetailsIResult(
        this List<ErrorOr.Error> errors,
        HttpContext httpContext
    )
    {
        ProblemDetails problemDetails = ErrorOrExtensions.BuildProblemDetails(errors, httpContext);
        return Results.Json(
            problemDetails,
            statusCode: problemDetails.Status ?? StatusCodes.Status500InternalServerError,
            contentType: "application/problem+json"
        );
    }

    public static IResult ToIResult<T>(
        this ErrorOr<T> result,
        HttpContext httpContext,
        Func<T, IResult> onSuccess
    )
    {
        if (!result.IsError)
            return onSuccess(result.Value);

        return result.Errors.ToProblemDetailsIResult(httpContext);
    }
}
