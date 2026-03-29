using System.Net;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration.Infrastructure;

public class RateLimitingTests : IClassFixture<AlbaRateLimitingFixture>
{
    private readonly AlbaRateLimitingFixture _fixture;
    private const int PermitLimit = AlbaRateLimitingFixture.PermitLimit;

    public RateLimitingTests(AlbaRateLimitingFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExceedingLimit_SameUser_Returns429()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = _fixture.Host.Server.CreateClient();
        IntegrationAuthHelper.Authenticate(client, username: "rl-exceed-user");

        for (var i = 0; i < PermitLimit; i++)
            (await client.GetAsync("/api/v1/products", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var throttled = await client.GetAsync("/api/v1/products", ct);
        throttled.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task WithinLimit_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = _fixture.Host.Server.CreateClient();
        IntegrationAuthHelper.Authenticate(client, username: "rl-within-user");

        for (var i = 0; i < PermitLimit; i++)
            (await client.GetAsync("/api/v1/products", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DifferentUsers_HaveIndependentBuckets()
    {
        var ct = TestContext.Current.CancellationToken;
        var clientA = _fixture.Host.Server.CreateClient();
        IntegrationAuthHelper.Authenticate(clientA, username: "rl-user-a");

        var clientB = _fixture.Host.Server.CreateClient();
        IntegrationAuthHelper.Authenticate(clientB, username: "rl-user-b");

        for (var i = 0; i < PermitLimit; i++)
            await clientA.GetAsync("/api/v1/products", ct);

        (await clientA.GetAsync("/api/v1/products", ct)).StatusCode.ShouldBe(
            HttpStatusCode.TooManyRequests
        );

        (await clientB.GetAsync("/api/v1/products", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
