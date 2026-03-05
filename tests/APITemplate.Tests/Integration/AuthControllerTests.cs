using System.Net;
using System.Net.Http.Json;
using APITemplate.Application.Features.Auth.Interfaces;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var mockProxy = new Mock<IAuthenticationProxy>();
        mockProxy
            .Setup(p => p.AuthenticateAsync("admin", "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse("mock-access-token", DateTime.UtcNow.AddHours(1)));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthenticationProxy>();
                services.AddSingleton(mockProxy.Object);
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Username = "admin", Password = "admin" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResponse.ShouldNotBeNull();
        tokenResponse.AccessToken.ShouldBe("mock-access-token");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var mockProxy = new Mock<IAuthenticationProxy>();
        mockProxy
            .Setup(p => p.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenResponse?)null);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthenticationProxy>();
                services.AddSingleton(mockProxy.Object);
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Username = "wrong", Password = "wrong" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Invalid username or password.");
    }
}
