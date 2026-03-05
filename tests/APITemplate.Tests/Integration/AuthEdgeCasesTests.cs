using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using APITemplate.Application.Common.Security;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration;

public class AuthEdgeCasesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEdgeCasesTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Request_WithTokenMissingTenantId_Returns401()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestAuthKeys.Issuer,
            audience: TestAuthKeys.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: TestAuthKeys.SigningCredentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithTokenEmptyTenantId_Returns401()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(CustomClaimTypes.TenantId, Guid.Empty.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestAuthKeys.Issuer,
            audience: TestAuthKeys.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: TestAuthKeys.SigningCredentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithExpiredToken_Returns401()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(CustomClaimTypes.TenantId, Guid.NewGuid().ToString()),
            new Claim("groups", "PlatformAdmin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestAuthKeys.Issuer,
            audience: TestAuthKeys.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: TestAuthKeys.SigningCredentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GraphQL_Mutation_WithoutToken_ReturnsGraphQLErrorPayload()
    {
        var mutation = """
            {
              "query": "mutation($input: CreateProductRequestInput!) { createProduct(input: $input) { id name } }",
              "variables": {
                "input": {
                  "name": "unauthorized-mutation",
                  "price": 1.23
                }
              }
            }
            """;

        using var content = new StringContent(mutation, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/graphql", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"errors\"");
    }

    [Fact]
    public async Task RequestContext_WhenCorrelationHeaderProvided_EchoesHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.Add("X-Correlation-Id", "corr-edge-123");

        var response = await _client.SendAsync(request);

        response.IsSuccessStatusCode.ShouldBeTrue();
        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe("corr-edge-123");
        response.Headers.GetValues("X-Trace-Id").Single().ShouldNotBeNullOrWhiteSpace();
        response.Headers.GetValues("X-Elapsed-Ms").Single().ShouldNotBeNullOrWhiteSpace();
    }
}
