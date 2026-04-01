using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Serilog.Context;
using SharedKernel.Application.Http;
using SharedKernel.Application.Security;
using SharedKernel.Infrastructure.Observability;

namespace SharedKernel.Api.Middleware;

/// <summary>
/// Enriches each request with correlation, tracing, timing, tenant metadata, and metrics tags.
/// </summary>
public sealed class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = ResolveCorrelationId(context);
        Stopwatch stopwatch = Stopwatch.StartNew();
        string traceId = Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;

        string? tenantId = context.User.FindFirstValue(SharedAuthConstants.Claims.TenantId);
        string effectiveTenantId = !string.IsNullOrWhiteSpace(tenantId) ? tenantId : string.Empty;

        if (!string.IsNullOrWhiteSpace(effectiveTenantId))
            Activity.Current?.SetTag(TelemetryTagKeys.TenantId, effectiveTenantId);

        context.Items[RequestContextConstants.ContextKeys.CorrelationId] = correlationId;
        context.Response.Headers[RequestContextConstants.Headers.CorrelationId] = correlationId;
        context.Response.Headers[RequestContextConstants.Headers.TraceId] = traceId;
        context.Response.Headers[RequestContextConstants.Headers.ElapsedMs] = "0";

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[RequestContextConstants.Headers.ElapsedMs] =
                stopwatch.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        try
        {
            using (
                LogContext.PushProperty(
                    RequestContextConstants.LogProperties.CorrelationId,
                    correlationId
                )
            )
            using (
                LogContext.PushProperty(
                    RequestContextConstants.LogProperties.TenantId,
                    effectiveTenantId
                )
            )
            {
                await _next(context);
            }
        }
        finally
        {
            IHttpMetricsTagsFeature? metricsTagsFeature =
                context.Features.Get<IHttpMetricsTagsFeature>();
            if (metricsTagsFeature is not null)
            {
                metricsTagsFeature.Tags.Add(
                    new(
                        TelemetryTagKeys.ApiSurface,
                        TelemetryApiSurfaceResolver.Resolve(context.Request.Path)
                    )
                );
                metricsTagsFeature.Tags.Add(
                    new(
                        TelemetryTagKeys.Authenticated,
                        context.User.Identity?.IsAuthenticated == true
                    )
                );
            }
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        string incoming = context
            .Request.Headers[RequestContextConstants.Headers.CorrelationId]
            .ToString();

        return !string.IsNullOrWhiteSpace(incoming) ? incoming : context.TraceIdentifier;
    }
}
