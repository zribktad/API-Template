using System.Text.Json;
using Alba;
using APITemplate.Tests.Integration;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration.Infrastructure;

public class ScalarAndOpenApiTests : IClassFixture<AlbaApiFixture>
{
    private readonly IAlbaHost _host;

    public ScalarAndOpenApiTests(AlbaApiFixture fixture)
    {
        _host = fixture.Host;
    }

    [Fact]
    public async Task OpenApi_Endpoint_ReturnsJsonDocument()
    {
        _ = await _host.Scenario(_ =>
        {
            _.Get.Url("/openapi/v1.json");
            _.StatusCodeShouldBeOk();
            _.ContentShouldContain("openapi");
            _.ContentShouldContain("paths");
            _.ContentShouldContain("ApiProblemDetails");
            _.ContentShouldContain("application/problem+json");
        });
    }

    [Fact]
    public async Task OpenApi_ContainsGlobalErrorResponsesForRestEndpoints()
    {
        var result = await _host.Scenario(_ =>
        {
            _.Get.Url("/openapi/v1.json");
            _.StatusCodeShouldBeOk();
        });

        var content = await result.ReadAsTextAsync();
        using var doc = JsonDocument.Parse(content);

        var paths = doc.RootElement.GetProperty("paths");
        var productReviewsPath = paths
            .EnumerateObject()
            .FirstOrDefault(p =>
                p.Name.Contains("productreviews", StringComparison.OrdinalIgnoreCase)
            )
            .Value;

        productReviewsPath.ValueKind.ShouldBe(JsonValueKind.Object);

        var productReviewsPost = productReviewsPath.GetProperty("post");
        var responses = productReviewsPost.GetProperty("responses");

        responses.TryGetProperty(StatusCodes.Status400BadRequest.ToString(), out _).ShouldBeTrue();
        responses
            .TryGetProperty(StatusCodes.Status401Unauthorized.ToString(), out _)
            .ShouldBeTrue();
        responses.TryGetProperty(StatusCodes.Status403Forbidden.ToString(), out _).ShouldBeTrue();
        responses.TryGetProperty(StatusCodes.Status404NotFound.ToString(), out _).ShouldBeTrue();
        responses
            .TryGetProperty(StatusCodes.Status500InternalServerError.ToString(), out _)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task OpenApi_ContainsOAuth2SecurityScheme()
    {
        var result = await _host.Scenario(_ =>
        {
            _.Get.Url("/openapi/v1.json");
            _.StatusCodeShouldBeOk();
        });

        var content = await result.ReadAsTextAsync();
        using var doc = JsonDocument.Parse(content);

        var components = doc.RootElement.GetProperty("components");
        var securitySchemes = components.GetProperty("securitySchemes");
        securitySchemes.TryGetProperty("OAuth2", out var oauth2).ShouldBeTrue();
        oauth2.GetProperty("type").GetString().ShouldBe("oauth2");
    }

    [Fact]
    public async Task Scalar_Endpoint_ReturnsHtml()
    {
        _ = await _host.Scenario(_ =>
        {
            _.Get.Url("/scalar/v1");
            _.StatusCodeShouldBeOk();
            _.ContentShouldContain("scalar");
            _.ContentShouldContain("authorizationUrl");
            _.ContentShouldContain("tokenUrl");
            _.ContentShouldContain("x-scalar-redirect-uri");
            _.ContentShouldContain("x-usePkce");
            _.ContentShouldContain("api-template-scalar");
        });
    }

    [Fact]
    public async Task GraphQL_Endpoint_IsAccessible()
    {
        _ = await _host.Scenario(_ =>
        {
            _.WithBearerToken(IntegrationAuthHelper.CreateTestToken());
            _.Post.Json(new { query = "{ __typename }" }).ToUrl("/graphql");
            _.StatusCodeShouldBeOk();
        });
    }
}
