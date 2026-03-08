using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using APITemplate.Tests.Integration.Helpers;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration;

public class CategoriesControllerTests
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CategoriesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullCrudFlow_WorksWithAuthentication()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client, tenantId: _tenantId);

        // 1. Get all - empty
        var getAllResponse = await _client.GetAsync("/api/v1/categories", ct);
        getAllResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var allCategories = await getAllResponse.Content.ReadFromJsonAsync<JsonElement>(TestJsonOptions.CaseInsensitive, ct);
        allCategories.GetProperty("items").EnumerateArray().ShouldBeEmpty();

        // 2. Create category
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/categories",
            new { Name = "Electronics", Description = "Electronic devices" },
            ct);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var categoryId = created.GetProperty("id").GetString();
        categoryId.ShouldNotBeNullOrWhiteSpace();
        created.GetProperty("name").GetString().ShouldBe("Electronics");
        created.GetProperty("description").GetString().ShouldBe("Electronic devices");

        // 3. Get by id
        var getByIdResponse = await _client.GetAsync($"/api/v1/categories/{categoryId}", ct);
        getByIdResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await getByIdResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        fetched.GetProperty("name").GetString().ShouldBe("Electronics");

        // 4. Update category
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/categories/{categoryId}",
            new { Name = "Updated Electronics", Description = "Updated description" },
            ct);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // 5. Verify update
        var verifyResponse = await _client.GetAsync($"/api/v1/categories/{categoryId}", ct);
        var updated = await verifyResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        updated.GetProperty("name").GetString().ShouldBe("Updated Electronics");
        updated.GetProperty("description").GetString().ShouldBe("Updated description");

        // 6. Delete category
        var deleteResponse = await _client.DeleteAsync($"/api/v1/categories/{categoryId}", ct);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // 7. Verify deletion
        var getDeletedResponse = await _client.GetAsync($"/api/v1/categories/{categoryId}", ct);
        getDeletedResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NonExistentCategory_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client, tenantId: _tenantId);

        var response = await _client.GetAsync($"/api/v1/categories/{Guid.NewGuid()}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_CategoryWithoutDescription_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client, tenantId: _tenantId);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/categories",
            new { Name = "Books" },
            ct);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        created.GetProperty("name").GetString().ShouldBe("Books");

        var descriptionElement = created.GetProperty("description");
        descriptionElement.ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Create_MultipleCategories_AllReturnedInGetAll()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client, tenantId: _tenantId);

        await _client.PostAsJsonAsync("/api/v1/categories", new { Name = "Category A" }, ct);
        await _client.PostAsJsonAsync("/api/v1/categories", new { Name = "Category B" }, ct);

        var response = await _client.GetAsync("/api/v1/categories", ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var categories = await response.Content.ReadFromJsonAsync<JsonElement>(TestJsonOptions.CaseInsensitive, ct);
        categories.GetProperty("items").EnumerateArray().Count().ShouldBeGreaterThanOrEqualTo(2);
        categories.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedEnvelope()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client, tenantId: _tenantId);

        await _client.PostAsJsonAsync("/api/v1/categories", new { Name = "Office Furniture", Description = "Desk and chair" }, ct);
        await _client.PostAsJsonAsync("/api/v1/categories", new { Name = "Kitchen Tools", Description = "Pans and knives" }, ct);

        var response = await _client.GetAsync("/api/v1/categories?pageNumber=1&pageSize=1&sortBy=name&sortDirection=asc", ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(TestJsonOptions.CaseInsensitive, ct);
        var items = payload.GetProperty("items").EnumerateArray().ToArray();

        items.Length.ShouldBe(1);
        payload.GetProperty("pageNumber").GetInt32().ShouldBe(1);
        payload.GetProperty("pageSize").GetInt32().ShouldBe(1);
        payload.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
    }
}
