using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel.Infrastructure.Observability;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Observability;

public sealed class HttpRouteResolverTests
{
    [Fact]
    public void ReplaceVersionToken_WhenRouteContainsApiVersionConstraint_ReplacesWithConcreteVersion()
    {
        string resolvedRoute = HttpRouteResolver.ReplaceVersionToken(
            "api/v{version:apiVersion}/products",
            new RouteValueDictionary { ["version"] = "1" }
        );

        resolvedRoute.ShouldBe("api/v1/products");
    }

    [Fact]
    public void ReplaceVersionToken_WhenVersionMissing_LeavesTemplateUnchanged()
    {
        string resolvedRoute = HttpRouteResolver.ReplaceVersionToken(
            "api/v{version:apiVersion}/products",
            new RouteValueDictionary()
        );

        resolvedRoute.ShouldBe("api/v{version:apiVersion}/products");
    }

    [Fact]
    public void Resolve_WhenEndpointTemplateMissing_FallsBackToRequestPath()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/api/v1/products";

        string resolvedRoute = HttpRouteResolver.Resolve(httpContext);

        resolvedRoute.ShouldBe("/api/v1/products");
    }
}
