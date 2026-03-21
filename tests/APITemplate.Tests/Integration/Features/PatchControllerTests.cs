using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using APITemplate.Application.Features.Product;
using APITemplate.Tests.Integration.Helpers;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Integration.Features;

public class PatchControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PatchControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Patch_ReplaceNameOnly_ChangesNamePreservesOtherFields()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client);

        // Create a product first
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                Items = new[]
                {
                    new
                    {
                        Name = "Patch Original",
                        Description = "Keep this",
                        Price = 50.00m,
                    },
                },
            },
            ct
        );
        var createBody = await createResponse.Content.ReadAsStringAsync(ct);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, createBody);
        var createBatch = JsonSerializer.Deserialize<BatchResponse>(
            createBody,
            TestJsonOptions.CaseInsensitive
        )!;
        var created = new { Id = createBatch.Results[0].Id!.Value };

        // Patch name only
        var patchJson = """[{"op": "replace", "path": "/name", "value": "Patch Updated"}]""";
        var patchContent = new StringContent(
            patchJson,
            Encoding.UTF8,
            "application/json-patch+json"
        );
        var patchResponse = await _client.PatchAsync(
            $"/api/v1/patch/products/{created.Id}",
            patchContent,
            ct
        );
        var patchBody = await patchResponse.Content.ReadAsStringAsync(ct);
        patchResponse.StatusCode.ShouldBe(HttpStatusCode.OK, patchBody);

        var patched = JsonSerializer.Deserialize<ProductResponse>(
            patchBody,
            TestJsonOptions.CaseInsensitive
        )!;
        patched.Name.ShouldBe("Patch Updated");
        patched.Description.ShouldBe("Keep this");
        patched.Price.ShouldBe(50.00m);
    }

    [Fact]
    public async Task Patch_ReplaceMultipleFields_AllUpdated()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                Items = new[]
                {
                    new
                    {
                        Name = "Multi Patch",
                        Description = "Original",
                        Price = 25.00m,
                    },
                },
            },
            ct
        );
        var createBody = await createResponse.Content.ReadAsStringAsync(ct);
        var createBatch = JsonSerializer.Deserialize<BatchResponse>(
            createBody,
            TestJsonOptions.CaseInsensitive
        )!;
        var created = new { Id = createBatch.Results[0].Id!.Value };

        var patchJson =
            """[{"op": "replace", "path": "/name", "value": "Multi Updated"}, {"op": "replace", "path": "/price", "value": 99.99}]""";
        var patchContent = new StringContent(
            patchJson,
            Encoding.UTF8,
            "application/json-patch+json"
        );
        var patchResponse = await _client.PatchAsync(
            $"/api/v1/patch/products/{created.Id}",
            patchContent,
            ct
        );
        var patchBody = await patchResponse.Content.ReadAsStringAsync(ct);
        patchResponse.StatusCode.ShouldBe(HttpStatusCode.OK, patchBody);

        var patched = JsonSerializer.Deserialize<ProductResponse>(
            patchBody,
            TestJsonOptions.CaseInsensitive
        )!;
        patched.Name.ShouldBe("Multi Updated");
        patched.Price.ShouldBe(99.99m);
    }

    [Fact]
    public async Task Patch_PriceToNegative_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                Items = new[]
                {
                    new
                    {
                        Name = "Negative Patch",
                        Description = "Test",
                        Price = 50.00m,
                    },
                },
            },
            ct
        );
        var createBody = await createResponse.Content.ReadAsStringAsync(ct);
        var createBatch = JsonSerializer.Deserialize<BatchResponse>(
            createBody,
            TestJsonOptions.CaseInsensitive
        )!;
        var created = new { Id = createBatch.Results[0].Id!.Value };

        var patchJson = """[{"op": "replace", "path": "/price", "value": -1}]""";
        var patchContent = new StringContent(
            patchJson,
            Encoding.UTF8,
            "application/json-patch+json"
        );
        var patchResponse = await _client.PatchAsync(
            $"/api/v1/patch/products/{created.Id}",
            patchContent,
            ct
        );

        patchResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_NonExistentProduct_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client);

        var patchJson = """[{"op": "replace", "path": "/name", "value": "Ghost"}]""";
        var patchContent = new StringContent(
            patchJson,
            Encoding.UTF8,
            "application/json-patch+json"
        );
        var patchResponse = await _client.PatchAsync(
            $"/api/v1/patch/products/{Guid.NewGuid()}",
            patchContent,
            ct
        );

        patchResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_RemoveDescription_SetsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        IntegrationAuthHelper.Authenticate(_client);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                Items = new[]
                {
                    new
                    {
                        Name = "Remove Desc",
                        Description = "To be removed",
                        Price = 10.00m,
                    },
                },
            },
            ct
        );
        var createBody = await createResponse.Content.ReadAsStringAsync(ct);
        var createBatch = JsonSerializer.Deserialize<BatchResponse>(
            createBody,
            TestJsonOptions.CaseInsensitive
        )!;
        var created = new { Id = createBatch.Results[0].Id!.Value };

        var patchJson = """[{"op": "remove", "path": "/description"}]""";
        var patchContent = new StringContent(
            patchJson,
            Encoding.UTF8,
            "application/json-patch+json"
        );
        var patchResponse = await _client.PatchAsync(
            $"/api/v1/patch/products/{created.Id}",
            patchContent,
            ct
        );
        var patchBody = await patchResponse.Content.ReadAsStringAsync(ct);
        patchResponse.StatusCode.ShouldBe(HttpStatusCode.OK, patchBody);

        var patched = JsonSerializer.Deserialize<ProductResponse>(
            patchBody,
            TestJsonOptions.CaseInsensitive
        )!;
        patched.Description.ShouldBeNull();
    }
}
