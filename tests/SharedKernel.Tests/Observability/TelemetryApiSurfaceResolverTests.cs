using Microsoft.AspNetCore.Http;
using SharedKernel.Infrastructure.Observability;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Observability;

public sealed class TelemetryApiSurfaceResolverTests
{
    [Theory]
    [InlineData("/graphql", TelemetrySurfaces.GraphQl)]
    [InlineData("/graphql/ui", TelemetrySurfaces.GraphQl)]
    [InlineData("/health", TelemetrySurfaces.Health)]
    [InlineData("/openapi/v1/openapi.json", TelemetrySurfaces.Documentation)]
    [InlineData("/scalar/v1", TelemetrySurfaces.Documentation)]
    [InlineData("/api/v1/products", TelemetrySurfaces.Rest)]
    [InlineData("/", TelemetrySurfaces.Rest)]
    public void Resolve_ReturnsExpectedSurface(string path, string expected)
    {
        string actual = TelemetryApiSurfaceResolver.Resolve(new PathString(path));

        actual.ShouldBe(expected);
    }
}
