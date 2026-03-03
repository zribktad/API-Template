using Microsoft.AspNetCore.Http;
using APITemplate.Domain.Exceptions;

namespace APITemplate.Api.ExceptionHandling;

public static class ApiProblemDetailsOptions
{
    public static void Configure(ProblemDetailsOptions options)
    {
        options.CustomizeProblemDetails = context =>
        {
            var extensions = context.ProblemDetails.Extensions;
            extensions["traceId"] = context.HttpContext.TraceIdentifier;

            var errorCode = extensions.TryGetValue("errorCode", out var existingErrorCode) && existingErrorCode is string existing
                ? existing
                : ErrorCatalog.General.Unknown;

            if (context.Exception is AppException appException
                && !string.IsNullOrWhiteSpace(appException.ErrorCode))
            {
                errorCode = appException.ErrorCode!;
            }
            else if (context.Exception is AppException appExceptionWithMetadata
                && appExceptionWithMetadata.Metadata is not null
                && appExceptionWithMetadata.Metadata.TryGetValue("errorCode", out var metadataErrorCode)
                && metadataErrorCode is string metadataValue
                && !string.IsNullOrWhiteSpace(metadataValue))
            {
                errorCode = metadataValue;
            }

            extensions["errorCode"] = errorCode;
            context.ProblemDetails.Type ??= $"https://api-template.local/errors/{errorCode}";
        };
    }
}
