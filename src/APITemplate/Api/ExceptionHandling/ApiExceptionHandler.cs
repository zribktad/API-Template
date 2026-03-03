using APITemplate.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.ExceptionHandling;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        // GraphQL has its own error handling pipeline.
        if (context.Request.Path.StartsWithSegments("/graphql"))
            return false;

        var (statusCode, title, detail, errorCode, metadata) = Resolve(exception);
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = BuildTypeUri(errorCode)
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        if (metadata is not null && metadata.Count > 0)
            problemDetails.Extensions["metadata"] = metadata;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, TraceId: {TraceId}",
                statusCode,
                errorCode,
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Handled application exception. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, TraceId: {TraceId}",
                statusCode,
                errorCode,
                context.TraceIdentifier);
        }

        context.Response.StatusCode = statusCode;
        var wasWritten = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = problemDetails
        });

        return wasWritten;
    }

    private static (int StatusCode, string Title, string Detail, string ErrorCode, IReadOnlyDictionary<string, object?>? Metadata) Resolve(Exception exception)
    {
        if (exception is AppException appException)
        {
            var (statusCode, title, defaultErrorCode) = MapToHttp(appException);
            var errorCode = ResolveErrorCode(appException, defaultErrorCode);

            return (
                statusCode,
                title,
                appException.Message,
                errorCode,
                appException.Metadata);
        }

        return (
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred.",
            ErrorCatalog.General.Unknown,
            null);
    }

    private static (int StatusCode, string Title, string ErrorCode) MapToHttp(AppException appException)
        => appException switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Bad Request", ErrorCatalog.General.ValidationFailed),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", ErrorCatalog.General.NotFound),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", ErrorCatalog.General.Conflict),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", ErrorCatalog.General.Unknown)
        };

    private static string ResolveErrorCode(AppException appException, string defaultErrorCode)
    {
        if (!string.IsNullOrWhiteSpace(appException.ErrorCode))
            return appException.ErrorCode!;

        if (appException.Metadata is not null
            && appException.Metadata.TryGetValue("errorCode", out var metadataErrorCode)
            && metadataErrorCode is string value
            && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return defaultErrorCode;
    }

    private static string BuildTypeUri(string errorCode)
        => $"https://api-template.local/errors/{errorCode}";
}
