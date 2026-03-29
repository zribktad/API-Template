using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Serilog;
using Serilog.Context;
using SharedKernel.Application.Security;

namespace SharedKernel.Api.Middleware;

public sealed class RequestContextMiddleware(
    RequestDelegate next,
    IDiagnosticContext diagnosticContext
)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        var correlationId =
            context.Request.Headers[RequestContextConstants.Headers.CorrelationId].FirstOrDefault()
            ?? context.TraceIdentifier;

        context.Response.OnStarting(() =>
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            context.Response.Headers[RequestContextConstants.Headers.CorrelationId] = correlationId;
            context.Response.Headers[RequestContextConstants.Headers.TraceId] =
                Activity.Current?.Id ?? context.TraceIdentifier;
            context.Response.Headers[RequestContextConstants.Headers.ElapsedMs] =
                elapsed.TotalMilliseconds.ToString("F1");

            return Task.CompletedTask;
        });

        using (
            LogContext.PushProperty(
                RequestContextConstants.LogProperties.CorrelationId,
                correlationId
            )
        )
        {
            await next(context);
        }

        var tenantId = context.User.FindFirstValue(SharedAuthConstants.Claims.TenantId);
        if (tenantId is not null)
        {
            diagnosticContext.Set(RequestContextConstants.LogProperties.TenantId, tenantId);
        }

        var metricsFeature = context.Features.Get<IHttpMetricsTagsFeature>();
        if (metricsFeature is not null)
        {
            var endpoint = context.GetEndpoint();
            var apiSurface =
                endpoint?.Metadata.GetMetadata<ApiSurfaceMetadata>()?.Surface ?? "rest";

            metricsFeature.Tags.Add(
                new KeyValuePair<string, object?>(
                    RequestContextConstants.MetricTags.ApiSurface,
                    apiSurface
                )
            );
            metricsFeature.Tags.Add(
                new KeyValuePair<string, object?>(
                    RequestContextConstants.MetricTags.Authenticated,
                    context.User.Identity?.IsAuthenticated ?? false
                )
            );
        }
    }
}

/// <summary>
/// Endpoint metadata that identifies the API surface type (e.g. "rest", "graphql", "grpc").
/// </summary>
public sealed class ApiSurfaceMetadata(string surface)
{
    public string Surface { get; } = surface;
}
