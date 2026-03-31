using ProductCatalog.Domain.Entities;
using Shouldly;
using Xunit;

namespace ProductCatalog.Tests.Domain.Entities;

public sealed class ProductTests
{
    [Fact]
    public void SoftDeleteProductDataLinks_RemovesAllLinks()
    {
        Product product = CreateProductWithLinks(3);
        product.ProductDataLinks.Count.ShouldBe(3);

        product.SoftDeleteProductDataLinks();

        product.ProductDataLinks.ShouldBeEmpty();
    }

    [Fact]
    public void SoftDeleteProductDataLinks_WithNoLinks_DoesNothing()
    {
        Product product = new() { Id = Guid.NewGuid(), Name = "Test" };

        product.SoftDeleteProductDataLinks();

        product.ProductDataLinks.ShouldBeEmpty();
    }

    [Fact]
    public void SyncProductDataLinks_RemovesLinksNotInTargetSet()
    {
        Guid keepId = Guid.NewGuid();
        Guid removeId = Guid.NewGuid();
        Product product = new() { Id = Guid.NewGuid(), Name = "Test" };
        product.ProductDataLinks.Add(ProductDataLink.Create(product.Id, keepId));
        product.ProductDataLinks.Add(ProductDataLink.Create(product.Id, removeId));

        HashSet<Guid> targetIds = new() { keepId };
        Dictionary<Guid, ProductDataLink> existingById = product.ProductDataLinks.ToDictionary(l =>
            l.ProductDataId
        );

        product.SyncProductDataLinks(targetIds, existingById);

        product.ProductDataLinks.Count.ShouldBe(1);
        product.ProductDataLinks.First().ProductDataId.ShouldBe(keepId);
    }

    [Fact]
    public void SyncProductDataLinks_AddsNewLinksNotInExisting()
    {
        Guid existingId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        Product product = new() { Id = Guid.NewGuid(), Name = "Test" };
        product.ProductDataLinks.Add(ProductDataLink.Create(product.Id, existingId));

        HashSet<Guid> targetIds = new() { existingId, newId };
        Dictionary<Guid, ProductDataLink> existingById = product.ProductDataLinks.ToDictionary(l =>
            l.ProductDataId
        );

        product.SyncProductDataLinks(targetIds, existingById);

        product.ProductDataLinks.Count.ShouldBe(2);
        product.ProductDataLinks.ShouldContain(l => l.ProductDataId == newId);
    }

    [Fact]
    public void Name_ThrowsOnEmpty()
    {
        Should.Throw<ArgumentException>(() => new Product { Id = Guid.NewGuid(), Name = "" });
    }

    [Fact]
    public void Price_ThrowsOnNegative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            Product product = new() { Id = Guid.NewGuid(), Name = "Test" };
            product.Price = -1m;
        });
    }

    private static Product CreateProductWithLinks(int count)
    {
        Product product = new() { Id = Guid.NewGuid(), Name = "Test Product" };
        for (int i = 0; i < count; i++)
            product.ProductDataLinks.Add(ProductDataLink.Create(product.Id, Guid.NewGuid()));
        return product;
    }
}
