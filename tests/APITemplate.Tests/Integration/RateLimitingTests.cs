using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration;

public class RateLimitingTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private const int PermitLimit = 3;

    public Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.Configure<RateLimitingOptions>(o =>
                {
                    o.PermitLimit = PermitLimit;
                    o.WindowMinutes = 1;
                });
            });
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task ExceedingLimit_SameUser_Returns429()
    {
        var client = _factory.CreateClient();
        IntegrationAuthHelper.Authenticate(client, username: "rl-exceed-user");

        for (var i = 0; i < PermitLimit; i++)
            (await client.GetAsync("/api/v1/products")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var throttled = await client.GetAsync("/api/v1/products");
        throttled.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task WithinLimit_ReturnsOk()
    {
        var client = _factory.CreateClient();
        IntegrationAuthHelper.Authenticate(client, username: "rl-within-user");

        for (var i = 0; i < PermitLimit; i++)
            (await client.GetAsync("/api/v1/products")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DifferentUsers_HaveIndependentBuckets()
    {
        var clientA = _factory.CreateClient();
        IntegrationAuthHelper.Authenticate(clientA, username: "rl-user-a");

        var clientB = _factory.CreateClient();
        IntegrationAuthHelper.Authenticate(clientB, username: "rl-user-b");

        // Exhaust user-a's bucket
        for (var i = 0; i < PermitLimit; i++)
            await clientA.GetAsync("/api/v1/products");

        (await clientA.GetAsync("/api/v1/products")).StatusCode
            .ShouldBe(HttpStatusCode.TooManyRequests);

        // user-b has their own independent bucket — not affected
        (await clientB.GetAsync("/api/v1/products")).StatusCode
            .ShouldBe(HttpStatusCode.OK);
    }
}
