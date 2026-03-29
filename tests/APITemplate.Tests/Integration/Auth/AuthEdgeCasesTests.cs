using APITemplate.Application.Common.Http;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration.Auth;

public class AuthEdgeCasesTests : IClassFixture<AlbaApiFixture>
{
    private readonly HttpClient _client;

    public AuthEdgeCasesTests(AlbaApiFixture fixture)
    {
        _client = fixture.Host.Server.CreateClient();
    }

    [Fact]
    public async Task RequestContext_WhenCorrelationHeaderProvided_EchoesHeader()
    {
        var ct = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.Add(RequestContextConstants.Headers.CorrelationId, "corr-edge-123");

        var response = await _client.SendAsync(request, ct);

        response.IsSuccessStatusCode.ShouldBeTrue();
        response
            .Headers.GetValues(RequestContextConstants.Headers.CorrelationId)
            .Single()
            .ShouldBe("corr-edge-123");
        response
            .Headers.GetValues(RequestContextConstants.Headers.TraceId)
            .Single()
            .ShouldNotBeNullOrWhiteSpace();
        response
            .Headers.GetValues(RequestContextConstants.Headers.ElapsedMs)
            .Single()
            .ShouldNotBeNullOrWhiteSpace();
    }
}
