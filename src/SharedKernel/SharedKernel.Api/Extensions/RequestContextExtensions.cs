using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;
using SharedKernel.Api.Middleware;

namespace SharedKernel.Api.Extensions;

public static class RequestContextExtensions
{
    public static WebApplication UseSharedRequestContext(this WebApplication app)
    {
        app.UseMiddleware<RequestContextMiddleware>();
        return app;
    }

    public static WebApplication UseSharedRequestLogging(this WebApplication app)
    {
        app.UseSharedRequestContext();
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = GetSerilogLevel;
            options.EnrichDiagnosticContext = EnrichDiagnosticContext;
        });
        return app;
    }

    private static LogEventLevel GetSerilogLevel(HttpContext context, double elapsed, Exception? ex)
    {
        if (ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return LogEventLevel.Information;
        if (context.Response.StatusCode >= 500)
            return LogEventLevel.Error;
        if (context.Response.StatusCode >= 400)
            return LogEventLevel.Warning;
        return LogEventLevel.Information;
    }

    private static void EnrichDiagnosticContext(
        IDiagnosticContext diagnosticContext,
        HttpContext context
    )
    {
        diagnosticContext.Set("RequestHost", context.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", context.Request.Scheme);
    }
}
