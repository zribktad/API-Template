using System.Net;
using System.Text.Json;
using APITemplate.Application.Common.Options;
using APITemplate.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Services;

public sealed class AuthentikAuthenticationProxyTests
{
    private static readonly AuthentikOptions TestOptions = new()
    {
        Authority = "https://authentik.local",
        ClientId = "test-client",
        ClientSecret = "test-secret",
        TokenEndpoint = "https://authentik.local/application/o/token/"
    };

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsTokenResponse()
    {
        var tokenJson = JsonSerializer.Serialize(new
        {
            access_token = "eyJ.test.token",
            expires_in = 3600,
            token_type = "Bearer"
        });

        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenJson, System.Text.Encoding.UTF8, "application/json")
        });

        var proxy = CreateProxy(handler);

        var result = await proxy.AuthenticateAsync("user", "pass");

        result.ShouldNotBeNull();
        result!.AccessToken.ShouldBe("eyJ.test.token");
        result.ExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidCredentials_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"invalid_grant\"}", System.Text.Encoding.UTF8, "application/json")
        });

        var proxy = CreateProxy(handler);

        var result = await proxy.AuthenticateAsync("user", "wrong");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_AuthentikUnavailable_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(new HttpRequestException("Connection refused"));

        var proxy = CreateProxy(handler);

        var result = await proxy.AuthenticateAsync("user", "pass");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_MalformedResponse_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        });

        var proxy = CreateProxy(handler);

        var result = await proxy.AuthenticateAsync("user", "pass");

        result.ShouldBeNull();
    }

    private static AuthentikAuthenticationProxy CreateProxy(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(TestOptions);
        return new AuthentikAuthenticationProxy(httpClient, options);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly HttpRequestException? _exception;

        public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;
        public FakeHttpMessageHandler(HttpRequestException exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception is not null)
                throw _exception;

            return Task.FromResult(_response!);
        }
    }
}
