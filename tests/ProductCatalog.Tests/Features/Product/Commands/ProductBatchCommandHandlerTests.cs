using Ardalis.Specification;
using Contracts.IntegrationEvents.ProductCatalog;
using Moq;
using ProductCatalog.Application.Common.Errors;
using ProductCatalog.Application.Features.Product.Commands;
using ProductCatalog.Application.Features.Product.DTOs;
using ProductCatalog.Application.Features.Product.Repositories;
using ProductCatalog.Application.Features.Product.Validation;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Entities.ProductData;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Domain.Interfaces;
using Shouldly;
using Wolverine;
using Xunit;
using CategoryEntity = ProductCatalog.Domain.Entities.Category;
using ProductEntity = ProductCatalog.Domain.Entities.Product;

namespace ProductCatalog.Tests.Features.Product.Commands;

public sealed class ProductBatchCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<IProductDataRepository> _productDataRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMessageBus> _busMock = new();

    public ProductBatchCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>(),
                    null
                )
            )
            .Returns<Func<Task>, CancellationToken, object?>((action, _, _) => action());

        _busMock
            .Setup(b => b.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);
    }

    [Fact]
    public async Task CreateHandleAsync_WhenReferencesAreMissing_ReturnsMergedBatchFailure()
    {
        Guid missingCategoryId = Guid.NewGuid();
        Guid missingProductDataId = Guid.NewGuid();
        CreateProductsCommand command = new(
            new CreateProductsRequest([
                new CreateProductRequest(
                    "Product",
                    "Desc",
                    10m,
                    missingCategoryId,
                    [missingProductDataId]
                ),
            ])
        );

        _categoryRepositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ISpecification<CategoryEntity>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);
        _productDataRepositoryMock
            .Setup(r =>
                r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        var (result, _) = await CreateProductsCommandHandler.HandleAsync(
            command,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _busMock.Object,
            new CreateProductRequestValidator(),
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.FailureCount.ShouldBe(1);
        result.Value.Failures.ShouldHaveSingleItem();
        result
            .Value.Failures[0]
            .Errors.ShouldBe([
                string.Format(ErrorCatalog.Categories.NotFoundMessage, missingCategoryId),
                string.Format(ErrorCatalog.ProductData.NotFoundMessage, missingProductDataId),
            ]);
        _productRepositoryMock.Verify(
            r =>
                r.AddRangeAsync(
                    It.IsAny<IEnumerable<ProductEntity>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _busMock.Verify(
            b => b.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateHandleAsync_WhenItemsAreValid_PersistsProductsAndPublishesEvents()
    {
        Guid categoryId = Guid.NewGuid();
        Guid productDataId = Guid.NewGuid();
        List<ProductEntity>? persistedProducts = null;
        CreateProductsCommand command = new(
            new CreateProductsRequest([
                new CreateProductRequest(
                    "Camera",
                    "Mirrorless",
                    499.99m,
                    categoryId,
                    [productDataId, productDataId]
                ),
                new CreateProductRequest("Lens", null, 199.99m, null, null),
            ])
        );

        _categoryRepositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ISpecification<CategoryEntity>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([new CategoryEntity { Id = categoryId, Name = "Photo" }]);
        _productDataRepositoryMock
            .Setup(r =>
                r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([new ImageProductData { Id = productDataId, Title = "Spec" }]);
        _productRepositoryMock
            .Setup(r =>
                r.AddRangeAsync(
                    It.IsAny<IEnumerable<ProductEntity>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<IEnumerable<ProductEntity>, CancellationToken>(
                (products, _) => persistedProducts = products.ToList()
            )
            .ReturnsAsync([]);

        var (result, _) = await CreateProductsCommandHandler.HandleAsync(
            command,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _busMock.Object,
            new CreateProductRequestValidator(),
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.SuccessCount.ShouldBe(2);
        persistedProducts.ShouldNotBeNull();
        persistedProducts.Count.ShouldBe(2);
        persistedProducts[0].CategoryId.ShouldBe(categoryId);
        persistedProducts[0].ProductDataLinks.Count.ShouldBe(1);
        persistedProducts[0].ProductDataLinks.Single().ProductDataId.ShouldBe(productDataId);
        persistedProducts[1].ProductDataLinks.ShouldBeEmpty();
        _busMock.Verify(
            b =>
                b.PublishAsync(
                    It.IsAny<ProductCreatedIntegrationEvent>(),
                    It.IsAny<DeliveryOptions?>()
                ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task LoadAsync_WhenProductIsMissing_ReturnsStopWithoutLookup()
    {
        Guid missingProductId = Guid.NewGuid();
        UpdateProductsCommand command = new(
            new UpdateProductsRequest([
                new UpdateProductItem(missingProductId, "Updated", null, 10m),
            ])
        );

        _productRepositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ISpecification<ProductEntity>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        var (continuation, lookup, _) = await UpdateProductsCommandHandler.LoadAsync(
            command,
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            new UpdateProductItemValidator(),
            CancellationToken.None
        );

        continuation.ShouldBe(HandlerContinuation.Stop);
        lookup.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenProductDataIdsAreProvided_SyncsLinksAndUpdatesProduct()
    {
        Guid productId = Guid.NewGuid();
        Guid oldProductDataId = Guid.NewGuid();
        Guid newProductDataId = Guid.NewGuid();
        ProductEntity product = new()
        {
            Id = productId,
            Name = "Old",
            Description = "Old Desc",
            Price = 10m,
            CategoryId = Guid.NewGuid(),
        };
        product.ProductDataLinks.Add(ProductDataLink.Create(productId, oldProductDataId));

        UpdateProductsCommand command = new(
            new UpdateProductsRequest([
                new UpdateProductItem(productId, "New", "New Desc", 25m, null, [newProductDataId]),
            ])
        );

        var (result, _) = await UpdateProductsCommandHandler.HandleAsync(
            command,
            new SharedKernel.Application.Batch.EntityLookup<ProductEntity>(
                new Dictionary<Guid, ProductEntity> { [productId] = product }
            ),
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.SuccessCount.ShouldBe(1);
        product.Name.ShouldBe("New");
        product.Description.ShouldBe("New Desc");
        product.Price.ShouldBe(25m);
        product.CategoryId.ShouldBeNull();
        product.ProductDataLinks.Count.ShouldBe(1);
        product.ProductDataLinks.Single().ProductDataId.ShouldBe(newProductDataId);
    }

    [Fact]
    public async Task HandleAsync_WhenProductDataIdsAreNull_LeavesExistingLinksUntouched()
    {
        Guid productId = Guid.NewGuid();
        Guid existingLinkId = Guid.NewGuid();
        ProductEntity product = new()
        {
            Id = productId,
            Name = "Old",
            Price = 10m,
        };
        product.ProductDataLinks.Add(ProductDataLink.Create(productId, existingLinkId));

        UpdateProductsCommand command = new(
            new UpdateProductsRequest([new UpdateProductItem(productId, "Renamed", null, 15m)])
        );

        var (result, _) = await UpdateProductsCommandHandler.HandleAsync(
            command,
            new SharedKernel.Application.Batch.EntityLookup<ProductEntity>(
                new Dictionary<Guid, ProductEntity> { [productId] = product }
            ),
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        product.ProductDataLinks.Count.ShouldBe(1);
        product.ProductDataLinks.Single().ProductDataId.ShouldBe(existingLinkId);
        _productRepositoryMock.Verify(
            r =>
                r.UpdateAsync(
                    It.Is<ProductEntity>(p => p.Id == productId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}
