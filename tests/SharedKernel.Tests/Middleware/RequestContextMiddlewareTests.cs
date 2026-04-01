using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using SharedKernel.Api.Middleware;
using SharedKernel.Application.Http;
using SharedKernel.Application.Security;
using SharedKernel.Infrastructure.Observability;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Middleware;

public sealed class RequestContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenHeaderProvided_EchoesCorrelationIdToResponse()
    {
        DefaultHttpContext context = CreateContext();
        context.Request.Headers[RequestContextConstants.Headers.CorrelationId] = "corr-123";

        RequestContextMiddleware sut = CreateSut();

        await sut.InvokeAsync(context);

        context
            .Response.Headers[RequestContextConstants.Headers.CorrelationId]
            .ToString()
            .ShouldBe("corr-123");
        context.Items[RequestContextConstants.ContextKeys.CorrelationId].ShouldBe("corr-123");
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderMissing_UsesTraceIdentifierAsCorrelationId()
    {
        DefaultHttpContext context = CreateContext();
        context.TraceIdentifier = "trace-xyz";

        RequestContextMiddleware sut = CreateSut();

        await sut.InvokeAsync(context);

        context
            .Response.Headers[RequestContextConstants.Headers.CorrelationId]
            .ToString()
            .ShouldBe("trace-xyz");
        context.Items[RequestContextConstants.ContextKeys.CorrelationId].ShouldBe("trace-xyz");
    }

    [Fact]
    public async Task InvokeAsync_PopulatesTraceIdAndElapsedHeaders()
    {
        DefaultHttpContext context = CreateContext();

        RequestContextMiddleware sut = CreateSut();

        await sut.InvokeAsync(context);

        context
            .Response.Headers[RequestContextConstants.Headers.TraceId]
            .ToString()
            .ShouldNotBeNullOrWhiteSpace();
        context
            .Response.Headers[RequestContextConstants.Headers.ElapsedMs]
            .ToString()
            .ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_TagsMetricsFeatureWithRestSurface()
    {
        DefaultHttpContext context = CreateContext();
        context.Request.Path = "/api/v1/products";
        FakeHttpMetricsTagsFeature metricsFeature = new();
        context.Features.Set<IHttpMetricsTagsFeature>(metricsFeature);

        RequestContextMiddleware sut = CreateSut();

        await sut.InvokeAsync(context);

        metricsFeature.Tags.ShouldContain(tag =>
            tag.Key == TelemetryTagKeys.ApiSurface && (string?)tag.Value == TelemetrySurfaces.Rest
        );
    }

    [Fact]
    public async Task InvokeAsync_TagsMetricsFeatureWithAuthenticatedStatus()
    {
        DefaultHttpContext context = CreateContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "TestAuth")
        );
        FakeHttpMetricsTagsFeature metricsFeature = new();
        context.Features.Set<IHttpMetricsTagsFeature>(metricsFeature);

        RequestContextMiddleware sut = CreateSut();

        await sut.InvokeAsync(context);

        metricsFeature
            .Tags.Any(tag => tag.Key == TelemetryTagKeys.Authenticated && Equals(tag.Value, true))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithTenantClaim_StoresCorrelationAndDoesNotFail()
    {
        DefaultHttpContext context = CreateContext();
        context.TraceIdentifier = "trace-with-tenant";
        string tenantId = Guid.NewGuid().ToString();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(SharedAuthConstants.Claims.TenantId, tenantId)],
                "TestAuth"
            )
        );

        RequestContextMiddleware sut = CreateSut();

        await sut.InvokeAsync(context);

        context
            .Items[RequestContextConstants.ContextKeys.CorrelationId]
            .ShouldBe("trace-with-tenant");
        context
            .Response.Headers[RequestContextConstants.Headers.CorrelationId]
            .ToString()
            .ShouldBe("trace-with-tenant");
    }

    private static RequestContextMiddleware CreateSut() =>
        new(async context =>
        {
            await context.Response.WriteAsync("ok");
        });

    private static DefaultHttpContext CreateContext()
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class FakeHttpMetricsTagsFeature : IHttpMetricsTagsFeature
    {
        public bool MetricsDisabled { get; set; }

        public ICollection<KeyValuePair<string, object?>> Tags { get; } =
            new List<KeyValuePair<string, object?>>();
    }
}
