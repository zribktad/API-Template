using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Api.ExceptionHandling;

namespace SharedKernel.Api.Extensions;

/// <summary>
/// Registers RFC 7807 ProblemDetails and the shared exception handler.
/// Call this in every API host — regardless of whether it uses EF Core.
/// </summary>
public static class ApiErrorHandlingExtensions
{
    public static IServiceCollection AddSharedApiErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(ApiProblemDetailsOptions.Configure);
        services.AddExceptionHandler<ApiExceptionHandler>();
        return services;
    }
}
