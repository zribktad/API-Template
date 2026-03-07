using APITemplate.Application.Common.Security;
using Microsoft.AspNetCore.Diagnostics;

namespace APITemplate.Api.Middleware;

/// <summary>
/// Middleware that enforces CSRF protection for cookie-authenticated requests.
/// </summary>
/// <remarks>
/// Only mutating HTTP methods (POST, PUT, PATCH, DELETE, …) are checked.
/// Safe methods (GET, HEAD, OPTIONS) and JWT Bearer-authenticated requests are
/// unconditionally allowed through — the header is required only when the
/// session cookie is the active authentication mechanism.
///
/// Clients must include <c>X-CSRF: 1</c> on every non-safe request.
/// The required header name and value are exposed via <c>GET /api/v1/bff/csrf</c>
/// so that SPAs can discover the contract at runtime.
/// </remarks>
public sealed class CsrfValidationMiddleware(RequestDelegate next, IProblemDetailsService problemDetailsService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Safe methods cannot cause state changes, so CSRF is not a concern.
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        // JWT Bearer requests carry their own proof of origin; skip CSRF check.
        var isCookieAuthenticated = context.User.Identities
            .Any(i => i.AuthenticationType == BffAuthenticationSchemes.Cookie);

        if (!isCookieAuthenticated)
        {
            await next(context);
            return;
        }

        // Cookie-authenticated mutating request — require the custom CSRF header.
        if (context.Request.Headers.TryGetValue(CsrfConstants.HeaderName, out var value) &&
            value == CsrfConstants.HeaderValue)
        {
            await next(context);
            return;
        }

        // Header missing or wrong value — reject with 403 and RFC 7807 problem details.
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails =
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = $"Cookie-authenticated requests must include the '{CsrfConstants.HeaderName}: {CsrfConstants.HeaderValue}' header."
            }
        });
    }
}
