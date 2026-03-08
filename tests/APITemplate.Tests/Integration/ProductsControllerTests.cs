using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration;

[Collection("Integration.ProductDataController")]
public class ProductsControllerTests
{
    private readonly HttpClient _client;
    private readonly Mock<IProductDataRepository> _productDataRepositoryMock;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _productDataRepositoryMock = factory.Services.GetRequiredService<Mock<IProductDataRepository>>();
        _productDataRepositoryMock.Reset();
    }

    [Fact]
    public async Task GetAll_WithValidToken_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client);

        var response = await _client.GetAsync("/api/v1/products", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        payload.GetProperty("page").GetProperty("items").ValueKind.ShouldBe(JsonValueKind.Array);
        payload.GetProperty("facets").ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public async Task CreateAndGetById_WithProductDataIds_RoundTripsIds()
    {
        var ct = TestContext.Current.CancellationToken;
        var productDataId = Guid.NewGuid();
        IntegrationAuthHelper.Authenticate(_client);

        _productDataRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImageProductData { Id = productDataId, Title = "Image" }]);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                name = "Product with data",
                description = "Test product",
                price = 25,
                productDataIds = new[] { productDataId, productDataId }
            },
            ct);

        var createBody = await createResponse.Content.ReadAsStringAsync(ct);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created, createBody);
        var created = JsonSerializer.Deserialize<JsonElement>(createBody);
        created.GetProperty("productDataIds").EnumerateArray().Select(x => x.GetGuid()).ShouldBe([productDataId]);

        var productId = created.GetProperty("id").GetGuid();

        var getResponse = await _client.GetAsync($"/api/v1/products/{productId}", ct);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        fetched.GetProperty("productDataIds").EnumerateArray().Select(x => x.GetGuid()).ShouldBe([productDataId]);
    }

    [Fact]
    public async Task Create_WithInvalidProductDataId_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                name = "Invalid product data",
                price = 25,
                productDataIds = new[] { "bad-id" }
            },
            ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, body);
    }

    [Fact]
    public async Task Update_WithoutProductDataIds_PreservesExistingLinks()
    {
        var ct = TestContext.Current.CancellationToken;
        var productDataId = Guid.NewGuid();
        IntegrationAuthHelper.Authenticate(_client);

        _productDataRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImageProductData { Id = productDataId, Title = "Image" }]);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                name = "Product with data",
                description = "Test product",
                price = 25,
                productDataIds = new[] { productDataId }
            },
            ct);

        var createBody = await createResponse.Content.ReadAsStringAsync(ct);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created, createBody);
        var created = JsonSerializer.Deserialize<JsonElement>(createBody);
        var productId = created.GetProperty("id").GetGuid();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/products/{productId}",
            new
            {
                name = "Renamed product",
                description = "Updated",
                price = 30
            },
            ct);

        var updateBody = await updateResponse.Content.ReadAsStringAsync(ct);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent, updateBody);

        var getResponse = await _client.GetAsync($"/api/v1/products/{productId}", ct);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        fetched.GetProperty("productDataIds").EnumerateArray().Select(x => x.GetGuid()).ShouldBe([productDataId]);
    }

    [Fact]
    public async Task GetAll_WithCategoryFilterAndFacets_ReturnsFilteredProductsAndFacetCounts()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client);

        var electronicsResponse = await _client.PostAsJsonAsync(
            "/api/v1/categories",
            new { Name = "Electronics", Description = "Devices and accessories" },
            ct);
        var booksResponse = await _client.PostAsJsonAsync(
            "/api/v1/categories",
            new { Name = "Books", Description = "Printed books" },
            ct);

        var electronics = await electronicsResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var books = await booksResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var electronicsId = electronics.GetProperty("id").GetGuid();
        var booksId = books.GetProperty("id").GetGuid();

        await _client.PostAsJsonAsync(
            "/api/v1/products",
            new { Name = "Wireless Mouse", Description = "Comfortable office mouse", Price = 30, CategoryId = electronicsId },
            ct);
        await _client.PostAsJsonAsync(
            "/api/v1/products",
            new { Name = "Wireless Keyboard", Description = "Mechanical office keyboard", Price = 80, CategoryId = electronicsId },
            ct);
        await _client.PostAsJsonAsync(
            "/api/v1/products",
            new { Name = "Fantasy Novel", Description = "Epic dragon story", Price = 15, CategoryId = booksId },
            ct);

        var response = await _client.GetAsync($"/api/v1/products?categoryIds={electronicsId}", ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var items = payload.GetProperty("page").GetProperty("items").EnumerateArray().ToArray();
        var categoryFacets = payload.GetProperty("facets").GetProperty("categories").EnumerateArray().ToArray();
        var priceBuckets = payload.GetProperty("facets").GetProperty("priceBuckets").EnumerateArray().ToArray();

        items.Length.ShouldBe(2);
        items.Select(item => item.GetProperty("name").GetString()).ShouldBe(["Wireless Mouse", "Wireless Keyboard"], ignoreOrder: true);
        categoryFacets.Length.ShouldBeGreaterThanOrEqualTo(2);
        categoryFacets[0].GetProperty("categoryName").GetString().ShouldBe("Electronics");
        categoryFacets[0].GetProperty("count").GetInt32().ShouldBe(2);
        priceBuckets.Single(bucket => bucket.GetProperty("label").GetString() == "0 - 50").GetProperty("count").GetInt32().ShouldBe(1);
        priceBuckets.Single(bucket => bucket.GetProperty("label").GetString() == "50 - 100").GetProperty("count").GetInt32().ShouldBe(1);
    }
}
