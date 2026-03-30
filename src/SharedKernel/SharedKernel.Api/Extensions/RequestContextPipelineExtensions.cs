using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;
using SharedKernel.Api.Middleware;

namespace SharedKernel.Api.Extensions;

/// <summary>
/// Registers correlation enrichment middleware and structured Serilog request logging.
/// </summary>
public static class RequestContextPipelineExtensions
{
    public static WebApplication UseRequestContextPipeline(this WebApplication app)
    {
        app.UseMiddleware<RequestContextMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.GetLevel = (httpContext, _, exception) =>
            {
                if (IsClientAbortedRequest(httpContext, exception))
                    return LogEventLevel.Information;

                if (exception is not null || httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                return LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            };
        });

        return app;
    }

    private static bool IsClientAbortedRequest(HttpContext httpContext, Exception? exception) =>
        exception is OperationCanceledException
        && httpContext.RequestAborted.IsCancellationRequested;
}
