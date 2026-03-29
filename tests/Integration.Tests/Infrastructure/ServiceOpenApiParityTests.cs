using Integration.Tests.Factories;
using Integration.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Integration.Tests.Infrastructure;

[Trait("Category", TestConstants.CategoryName)]
[Collection(TestConstants.CollectionName)]
public sealed class ServiceOpenApiParityTests : IAsyncLifetime
{
    private readonly SharedContainers _containers;
    private IdentityServiceFactory _identityFactory = null!;
    private ProductCatalogServiceFactory _productCatalogFactory = null!;
    private ReviewsServiceFactory _reviewsFactory = null!;

    public ServiceOpenApiParityTests(SharedContainers containers)
    {
        _containers = containers;
    }

    public async ValueTask InitializeAsync()
    {
        _identityFactory = new IdentityServiceFactory(_containers);
        _productCatalogFactory = new ProductCatalogServiceFactory(_containers);
        _reviewsFactory = new ReviewsServiceFactory(_containers);

        await Task.WhenAll(
            _identityFactory.InitializeAsync().AsTask(),
            _productCatalogFactory.InitializeAsync().AsTask(),
            _reviewsFactory.InitializeAsync().AsTask()
        );
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _reviewsFactory.DisposeAsync().AsTask(),
            _productCatalogFactory.DisposeAsync().AsTask(),
            _identityFactory.DisposeAsync().AsTask()
        );
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("product-catalog")]
    [InlineData("reviews")]
    public async Task Service_OpenApi_IncludesOAuthAndProblemDetails(string serviceName)
    {
        HttpClient client = serviceName switch
        {
            "identity" => _identityFactory.CreateClient(),
            "product-catalog" => _productCatalogFactory.CreateClient(),
            "reviews" => _reviewsFactory.CreateClient(),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceName), serviceName, null),
        };

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
        response.IsSuccessStatusCode.ShouldBeTrue();

        string content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("OAuth2");
        content.ShouldContain("oauth2");
        content.ShouldContain("ApiProblemDetails");
        content.ShouldContain("application/problem+json");
    }
}
