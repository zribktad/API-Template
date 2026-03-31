using System.Reflection;
using Microsoft.Extensions.Logging;
using SharedKernel.Api.ExceptionHandling;
using SharedKernel.Infrastructure.Logging;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Logging;

public sealed class ApiExceptionHandlerLogsTests
{
    [Fact]
    public void UnhandledException_UsesExpectedMessageAndClassifications()
    {
        MethodInfo method = typeof(ApiExceptionHandlerLogs).GetMethod(
            nameof(ApiExceptionHandlerLogs.UnhandledException)
        )!;

        LoggerMessageAttribute loggerMessage = method.GetCustomAttribute<LoggerMessageAttribute>()!;

        loggerMessage.Level.ShouldBe(LogLevel.Error);
        loggerMessage.Message.ShouldBe(
            "Unhandled exception. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, TraceId: {TraceId}"
        );

        method.GetParameters()[3].GetCustomAttribute<SensitiveDataAttribute>().ShouldNotBeNull();
        method.GetParameters()[4].GetCustomAttribute<PersonalDataAttribute>().ShouldNotBeNull();
    }

    [Fact]
    public void HandledApplicationException_UsesExpectedMessageAndClassifications()
    {
        MethodInfo method = typeof(ApiExceptionHandlerLogs).GetMethod(
            nameof(ApiExceptionHandlerLogs.HandledApplicationException)
        )!;

        LoggerMessageAttribute loggerMessage = method.GetCustomAttribute<LoggerMessageAttribute>()!;

        loggerMessage.Level.ShouldBe(LogLevel.Warning);
        loggerMessage.Message.ShouldBe(
            "Handled application exception. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, TraceId: {TraceId}"
        );

        method.GetParameters()[3].GetCustomAttribute<SensitiveDataAttribute>().ShouldNotBeNull();
        method.GetParameters()[4].GetCustomAttribute<PersonalDataAttribute>().ShouldNotBeNull();
    }
}
