using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration;

public class AuthenticatedCrudTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthenticatedCrudTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullCrudFlow_WorksWithAuthentication()
    {
        var tenantId = await GetDefaultTenantIdAsync();
        IntegrationAuthHelper.Authenticate(_client, Guid.NewGuid(), tenantId);

        // 1. Get all - empty
        var getAllResponse = await _client.GetAsync("/api/v1/products");
        getAllResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var pagedEmpty = await getAllResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var emptyList = pagedEmpty.GetProperty("items").EnumerateArray().ToArray();
        emptyList.ShouldBeEmpty();

        // 2. Create product
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new { Name = "Test Product", Description = "A description", Price = 29.99 });

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = created.GetProperty("id").GetString();
        productId.ShouldNotBeNullOrWhiteSpace();
        created.GetProperty("name").GetString().ShouldBe("Test Product");
        created.GetProperty("price").GetDecimal().ShouldBe(29.99m);

        // 3. Get by id
        var getByIdResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        getByIdResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await getByIdResponse.Content.ReadFromJsonAsync<JsonElement>();
        fetched.GetProperty("name").GetString().ShouldBe("Test Product");

        // 4. Update product
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/products/{productId}",
            new { Name = "Updated Product", Description = "Updated desc", Price = 39.99 });

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // 5. Verify update
        var verifyResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        var updated = await verifyResponse.Content.ReadFromJsonAsync<JsonElement>();
        updated.GetProperty("name").GetString().ShouldBe("Updated Product");
        updated.GetProperty("price").GetDecimal().ShouldBe(39.99m);

        // 6. Delete product
        var deleteResponse = await _client.DeleteAsync($"/api/v1/products/{productId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // 7. Verify deletion
        var getDeletedResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        getDeletedResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NonExistentProduct_ReturnsNotFound()
    {
        var tenantId = await GetDefaultTenantIdAsync();
        IntegrationAuthHelper.Authenticate(_client, Guid.NewGuid(), tenantId);

        var response = await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_MultipleProducts_AllReturnedInGetAll()
    {
        var tenantId = await GetDefaultTenantIdAsync();
        IntegrationAuthHelper.Authenticate(_client, Guid.NewGuid(), tenantId);

        await _client.PostAsJsonAsync("/api/v1/products",
            new { Name = "Product A", Price = 10.0 });
        await _client.PostAsJsonAsync("/api/v1/products",
            new { Name = "Product B", Price = 20.0 });

        var response = await _client.GetAsync("/api/v1/products");
        var pagedResponse = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var products = pagedResponse.GetProperty("items").EnumerateArray().ToArray();

        products.ShouldNotBeNull();
        products.Length.ShouldBeGreaterThanOrEqualTo(2);
    }

    private async Task<Guid> GetDefaultTenantIdAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Code == "default");
        return tenant.Id;
    }
}
