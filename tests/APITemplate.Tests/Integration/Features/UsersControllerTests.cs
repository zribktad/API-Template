using System.Net;
using System.Net.Http.Json;
using APITemplate.Domain.Enums;
using APITemplate.Tests.Integration.Helpers;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration.Features;

public class UsersControllerTests : IClassFixture<AlbaApiFixture>
{
    private readonly AlbaApiFixture _fixture;
    private readonly HttpClient _client;

    public UsersControllerTests(AlbaApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Host.Server.CreateClient();
    }

    [Fact]
    public async Task GetMe_WithAuthenticatedNonAdminUser_ReturnsCurrentUser()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tenant, user) = await IntegrationAuthHelper.SeedTenantUserAsync(
            _fixture.Host.Services,
            username: "regular-user",
            email: "regular-user@example.com",
            ct: ct
        );

        IntegrationAuthHelper.Authenticate(
            _client,
            user.Id,
            tenant.Id,
            user.Username,
            UserRole.User
        );

        var response = await _client.GetAsync("/api/v1/users/me", ct);
        var payload = await response.Content.ReadFromJsonAsync<UserResponse>(
            TestJsonOptions.CaseInsensitive,
            ct
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload!.Id.ShouldBe(user.Id);
        payload.Username.ShouldBe(user.Username);
        payload.Email.ShouldBe(user.Email);
    }

    [Fact]
    public async Task GetAll_WithAuthenticatedNonAdminUser_ReturnsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client, role: UserRole.User);

        var response = await _client.GetAsync("/api/v1/users", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
