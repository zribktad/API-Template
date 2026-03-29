using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration.Infrastructure;

public class CorsTests : IClassFixture<AlbaApiFixture>
{
    private readonly HttpClient _client;

    public CorsTests(AlbaApiFixture fixture)
    {
        _client = fixture.Host.Server.CreateClient();
    }

    [Theory]
    [InlineData("http://localhost:3000", true)]
    [InlineData("http://evil.example", false)]
    public async Task Preflight_Graphql_ReflectsOriginPolicy(string origin, bool expectAllowedCors)
    {
        var ct = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Options, "/graphql");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await _client.SendAsync(request, ct);

        if (expectAllowedCors)
        {
            response.IsSuccessStatusCode.ShouldBeTrue();
            response
                .Headers.TryGetValues("Access-Control-Allow-Origin", out var allowedOrigins)
                .ShouldBeTrue();
            allowedOrigins!.Single().ShouldBe(origin);

            response
                .Headers.TryGetValues("Access-Control-Allow-Credentials", out var credValues)
                .ShouldBeTrue();
            credValues!.Single().ShouldBe("true");
        }
        else
        {
            response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData("/openapi/v1.json", true)]
    [InlineData("/api/v1/products", false)]
    public async Task Get_FromAllowedOrigin_ReturnsCorsHeaders(
        string path,
        bool requireSuccessStatusCode
    )
    {
        var ct = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await _client.SendAsync(request, ct);

        if (requireSuccessStatusCode)
        {
            response.IsSuccessStatusCode.ShouldBeTrue();
        }

        response
            .Headers.TryGetValues("Access-Control-Allow-Origin", out var allowedOrigins)
            .ShouldBeTrue();
        allowedOrigins!.Single().ShouldBe("http://localhost:3000");
        response
            .Headers.TryGetValues("Access-Control-Allow-Credentials", out var credValues)
            .ShouldBeTrue();
        credValues!.Single().ShouldBe("true");
    }
}
