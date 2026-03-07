using System.Net.Mime;
using System.Text.Json;
using APITemplate.Application.Common.Security;

namespace APITemplate.Api.Middleware;

public sealed class CsrfValidationMiddleware(RequestDelegate next)
{
    private const string CsrfHeaderName = "X-CSRF";
    private const string CsrfHeaderValue = "1";

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        var isCookieAuthenticated = context.User.Identities
            .Any(i => i.AuthenticationType == BffAuthenticationSchemes.Cookie);

        if (!isCookieAuthenticated)
        {
            await next(context);
            return;
        }

        if (context.Request.Headers.TryGetValue(CsrfHeaderName, out var value) &&
            value == CsrfHeaderValue)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            title = "Forbidden",
            status = 403,
            detail = $"Cookie-authenticated requests must include the '{CsrfHeaderName}: {CsrfHeaderValue}' header."
        }));
    }
}
