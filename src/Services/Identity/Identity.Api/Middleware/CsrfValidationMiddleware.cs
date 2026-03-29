using Identity.Application.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Middleware;

/// <summary>
/// Validates the custom CSRF header on state-changing requests authenticated via the BFF cookie scheme.
/// Safe methods (GET, HEAD, OPTIONS) and Bearer-authenticated requests are allowed through.
/// </summary>
public sealed class CsrfValidationMiddleware
{
    private readonly RequestDelegate _next;

    public CsrfValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string method = context.Request.Method;

        if (
            HttpMethods.IsGet(method)
            || HttpMethods.IsHead(method)
            || HttpMethods.IsOptions(method)
        )
        {
            await _next(context);
            return;
        }

        string? authHeader = context.Request.Headers.Authorization;
        if (
            authHeader is not null
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        )
        {
            await _next(context);
            return;
        }

        bool isCookieAuthenticated = context.User.Identities.Any(i =>
            i.AuthenticationType == AuthConstants.BffSchemes.Cookie
        );

        if (!isCookieAuthenticated)
        {
            await _next(context);
            return;
        }

        string? csrfHeader = context.Request.Headers[AuthConstants.Csrf.HeaderName];

        if (csrfHeader == AuthConstants.Csrf.HeaderValue)
        {
            await _next(context);
            return;
        }

        await WriteCsrfForbiddenResponseAsync(context);
    }

    private static async Task WriteCsrfForbiddenResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = $"Missing or invalid '{AuthConstants.Csrf.HeaderName}' header.",
            Instance = context.Request.Path,
        };

        IProblemDetailsService? problemDetailsService =
            context.RequestServices.GetService<IProblemDetailsService>();

        if (problemDetailsService is not null)
        {
            await problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext { HttpContext = context, ProblemDetails = problemDetails }
            );
            return;
        }

        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
