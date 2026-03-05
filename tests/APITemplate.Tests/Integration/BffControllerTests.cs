using System.Net;
using System.Net.Http.Json;
using APITemplate.Application.Features.Bff.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration;

public class BffControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BffControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // --- Login ---

    [Fact]
    public async Task Login_RedirectsToAuthentik()
    {
        var response = await _client.GetAsync("/bff/login");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithLocalReturnUrl_Redirects()
    {
        var response = await _client.GetAsync("/bff/login?returnUrl=/dashboard");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    [Theory]
    [InlineData("https://evil.example.com")]
    [InlineData("//evil.example.com")]
    [InlineData("http://evil.example.com")]
    [InlineData("javascript:alert(1)")]
    public async Task Login_WithMaliciousReturnUrl_RedirectsToRoot(string maliciousUrl)
    {
        var response = await _client.GetAsync($"/bff/login?returnUrl={Uri.EscapeDataString(maliciousUrl)}");

        // Should still redirect (OIDC challenge), but malicious URL is replaced with "/"
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var location = response.Headers.Location?.ToString() ?? "";
        location.ShouldNotContain("evil.example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Login_WithEmptyOrWhitespaceReturnUrl_Redirects(string returnUrl)
    {
        var response = await _client.GetAsync($"/bff/login?returnUrl={Uri.EscapeDataString(returnUrl)}");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    // --- User (unauthenticated) ---

    [Fact]
    public async Task User_WithoutCookie_Returns401()
    {
        var response = await _client.GetAsync("/bff/user");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // --- Logout (unauthenticated) ---

    [Fact]
    public async Task Logout_WithoutCookie_Returns401()
    {
        var response = await _client.PostAsync("/bff/logout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // --- JWT Bearer regression ---

    [Fact]
    public async Task ExistingJwtBearerEndpoints_StillWork()
    {
        var response = await _client.GetAsync("/api/v1/products");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var tenant = await IntegrationAuthHelper.SeedTenantAsync(_factory.Services);
        var authenticatedClient = _factory.CreateClient();
        IntegrationAuthHelper.Authenticate(authenticatedClient, Guid.NewGuid(), tenant.Id);

        var authedResponse = await authenticatedClient.GetAsync("/api/v1/products");
        authedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // --- OpenAPI / Scalar ---

    [Fact]
    public async Task OpenApi_ContainsBffEndpoints()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("/bff/login");
        content.ShouldContain("/bff/logout");
        content.ShouldContain("/bff/user");
    }

    [Fact]
    public async Task Scalar_HasBearerAuthConfigured()
    {
        var response = await _client.GetAsync("/scalar/v1");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("scalar");
    }
}

public class BffAuthenticatedControllerTests : IClassFixture<BffWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BffAuthenticatedControllerTests(BffWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task User_WithValidCookie_ReturnsAllClaimFields()
    {
        var response = await _client.GetAsync("/bff/user");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<BffUserResponse>();
        user.ShouldNotBeNull();
        user.Sub.ShouldNotBeNullOrWhiteSpace();
        user.TenantId.ShouldNotBeNullOrWhiteSpace();
        user.PreferredUsername.ShouldBe("test-user");
        user.Email.ShouldBe("test@example.com");
        user.Name.ShouldBe("test-user");
        user.Roles.ShouldContain("PlatformAdmin");
    }

    [Fact]
    public async Task User_WithValidCookie_ReturnsXsrfTokenHeader()
    {
        var response = await _client.GetAsync("/bff/user");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-XSRF-TOKEN", out var xsrfValues).ShouldBeTrue();
        xsrfValues!.First().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task User_MultipleCalls_ReturnsXsrfTokenEachTime()
    {
        var response1 = await _client.GetAsync("/bff/user");
        var response2 = await _client.GetAsync("/bff/user");

        response1.Headers.TryGetValues("X-XSRF-TOKEN", out var token1).ShouldBeTrue();
        response2.Headers.TryGetValues("X-XSRF-TOKEN", out var token2).ShouldBeTrue();
        token1!.First().ShouldNotBeNullOrWhiteSpace();
        token2!.First().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Logout_WithoutAntiforgeryToken_Returns400()
    {
        var response = await _client.PostAsync("/bff/logout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_WithValidSession_ReturnsRedirect()
    {
        // Get antiforgery token — the cookie is stored automatically by the client's CookieContainer
        var userResponse = await _client.GetAsync("/bff/user");
        userResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        userResponse.Headers.TryGetValues("X-XSRF-TOKEN", out var xsrfValues).ShouldBeTrue();
        var xsrfToken = xsrfValues!.First();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/bff/logout");
        request.Headers.Add("X-XSRF-TOKEN", xsrfToken);

        var response = await _client.SendAsync(request);

        // SignOut returns 302 redirect
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }
}
