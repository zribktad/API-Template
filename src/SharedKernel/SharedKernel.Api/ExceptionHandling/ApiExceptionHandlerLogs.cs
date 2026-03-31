using Microsoft.Extensions.Logging;
using SharedKernel.Infrastructure.Logging;

namespace SharedKernel.Api.ExceptionHandling;

internal static partial class ApiExceptionHandlerLogs
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Unhandled exception. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, TraceId: {TraceId}"
    )]
    public static partial void UnhandledException(
        this ILogger logger,
        Exception exception,
        int statusCode,
        [SensitiveDataAttribute] string errorCode,
        [PersonalDataAttribute] string traceId
    );

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Handled application exception. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, TraceId: {TraceId}"
    )]
    public static partial void HandledApplicationException(
        this ILogger logger,
        Exception exception,
        int statusCode,
        [SensitiveDataAttribute] string errorCode,
        [PersonalDataAttribute] string traceId
    );
}
