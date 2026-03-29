using System.Net;
using Integration.Tests.Factories;
using Shouldly;
using Xunit;

namespace Integration.Tests.Infrastructure;

public sealed class GatewayScalarAndOpenApiTests : IClassFixture<GatewayServiceFactory>
{
    private readonly HttpClient _client;

    public GatewayScalarAndOpenApiTests(GatewayServiceFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Scalar_Endpoint_ContainsAllServiceDocumentsAndOAuthConfig()
    {
        HttpResponseMessage response = await _client.GetAsync("/scalar/v1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("api-template-scalar");
        content.ShouldContain("authorizationUrl");
        content.ShouldContain("tokenUrl");
    }

    [Theory]
    [InlineData("/openapi/identity.json")]
    [InlineData("/openapi/product-catalog.json")]
    [InlineData("/openapi/reviews.json")]
    [InlineData("/openapi/file-storage.json")]
    [InlineData("/openapi/notifications.json")]
    [InlineData("/openapi/background-jobs.json")]
    [InlineData("/openapi/webhooks.json")]
    public async Task OpenApi_GatewayRoutes_AreRegistered(string openApiPath)
    {
        HttpResponseMessage response = await _client.GetAsync(openApiPath);

        // Upstream service is intentionally unavailable in this test host, so
        // YARP should produce a proxy failure instead of a missing route.
        (
            response.StatusCode == HttpStatusCode.BadGateway
            || response.StatusCode == HttpStatusCode.ServiceUnavailable
        ).ShouldBeTrue();
    }
}
