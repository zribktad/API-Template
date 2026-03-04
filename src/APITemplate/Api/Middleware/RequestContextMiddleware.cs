using System.Diagnostics;
using Serilog.Context;

namespace APITemplate.Api.Middleware;

/// <summary>
/// Adds per-request context metadata used by logs and clients.
/// </summary>
/// <remarks>
/// In the current pipeline ordering this middleware runs after
/// <c>app.UseExceptionHandler()</c>, so thrown exceptions are still wrapped by
/// global exception handling while correlation and timing headers are maintained.
/// </remarks>
public sealed class RequestContextMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        var sw = Stopwatch.StartNew();
        context.Items[CorrelationIdItemKey] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;
        context.Response.Headers["X-Elapsed-Ms"] = "0";

        // Response headers are finalized here so elapsed time reflects full downstream execution.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Elapsed-Ms"] = sw.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationIdHeader].ToString();
        if (!string.IsNullOrWhiteSpace(incoming))
            return incoming;

        return context.TraceIdentifier;
    }
}
