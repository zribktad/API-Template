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

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("accessToken");
    }
}
