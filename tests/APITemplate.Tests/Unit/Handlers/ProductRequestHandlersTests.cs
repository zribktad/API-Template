using APITemplate.Application.Common.DTOs;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product;
using APITemplate.Application.Features.Product.DTOs;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.Product.Specifications;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Entities.ProductData;
using APITemplate.Domain.Interfaces;
using APITemplate.Domain.Options;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Handlers;

public class ProductRequestHandlersTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IProductDataRepository> _productDataRepositoryMock;
    private readonly Mock<IProductDataLinkRepository> _productDataLinkRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEventPublisher> _publisherMock;
    private readonly Mock<IValidator<CreateProductRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateProductItem>> _updateValidatorMock;

    public ProductRequestHandlersTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _productDataRepositoryMock = new Mock<IProductDataRepository>();
        _productDataLinkRepositoryMock = new Mock<IProductDataLinkRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publisherMock = new Mock<IEventPublisher>();
        _createValidatorMock = new Mock<IValidator<CreateProductRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateProductItem>>();
        _unitOfWorkMock.SetupImmediateTransactionExecution();
        _unitOfWorkMock.SetupImmediateTransactionExecution<Product>();

        // Default: validation passes
        _createValidatorMock
            .Setup(v =>
                v.ValidateAsync(It.IsAny<CreateProductRequest>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ValidationResult());
        _updateValidatorMock
            .Setup(v =>
                v.ValidateAsync(It.IsAny<UpdateProductItem>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ValidationResult());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetByIdAsync_ReturnsExpectedResult(bool productExists)
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = Guid.NewGuid();
        ProductResponse? response = productExists
            ? new ProductResponse(
                productId,
                "Test Product",
                "A test product",
                9.99m,
                Guid.NewGuid(),
                DateTime.UtcNow,
                []
            )
            : null;

        _repositoryMock
            .Setup(r =>
                r.FirstOrDefaultAsync(
                    It.IsAny<ProductByIdSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(response);

        var sut = new GetProductByIdQueryHandler(_repositoryMock.Object);
        var result = await sut.HandleAsync(new GetProductByIdQuery(productId), ct);

        if (productExists)
        {
            result.ShouldNotBeNull();
            result!.Name.ShouldBe("Test Product");
            result.Price.ShouldBe(9.99m);
        }
        else
        {
            result.ShouldBeNull();
        }
    }

    [Fact]
    public async Task BatchCreateAsync_ReturnsCreatedProduct()
    {
        var request = new CreateProductRequest("New Product", "Description", 19.99m);
        var batchRequest = new CreateProductsRequest([request]);

        var sut = new CreateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _createValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        result.FailureCount.ShouldBe(0);
        result.Failures.ShouldBeEmpty();

        _repositoryMock.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<Product>>(ps => ps.Count() == 1),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TransactionOptions?>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task BatchCreateAsync_WithProductDataIds_NormalizesAndStoresUniqueLinks()
    {
        var productDataId = Guid.NewGuid();
        var request = new CreateProductRequest(
            "New Product",
            "Description",
            19.99m,
            null,
            [productDataId, productDataId]
        );
        var batchRequest = new CreateProductsRequest([request]);

        _productDataRepositoryMock
            .Setup(r =>
                r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([new ImageProductData { Id = productDataId, Title = "Image" }]);

        var sut = new CreateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _createValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        result.Failures.ShouldBeEmpty();

        _repositoryMock.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<Product>>(ps =>
                        ps.Single().ProductDataLinks.Count == 1
                        && ps.Single().ProductDataLinks.Single().ProductDataId == productDataId
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task BatchCreateAsync_WithCategoryId_ValidatesCategory()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Test" };
        var request = new CreateProductRequest("New Product", "Description", 19.99m, categoryId);
        var batchRequest = new CreateProductsRequest([request]);

        _categoryRepositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<Application.Features.Category.Specifications.CategoriesByIdsSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([category]);

        var sut = new CreateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _createValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        result.Failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task BatchCreateAsync_WithNonExistentCategory_ReturnsFailure()
    {
        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequest("New Product", "Description", 19.99m, categoryId);
        var batchRequest = new CreateProductsRequest([request]);

        _categoryRepositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<Application.Features.Category.Specifications.CategoriesByIdsSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        var sut = new CreateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _createValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.FailureCount.ShouldBe(1);
        result.Failures[0].Errors.ShouldContain(e => e.Contains("Category"));
    }

    [Fact]
    public async Task BatchCreateAsync_WithMissingProductData_ReturnsFailure()
    {
        var productDataId = Guid.NewGuid();
        var request = new CreateProductRequest(
            "New Product",
            "Description",
            19.99m,
            null,
            [productDataId]
        );
        var batchRequest = new CreateProductsRequest([request]);

        _productDataRepositoryMock
            .Setup(r =>
                r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        var sut = new CreateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _createValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.FailureCount.ShouldBe(1);
        result.Failures[0].Errors.ShouldContain(e => e.Contains("Product data not found"));
    }

    [Fact]
    public async Task BatchUpdateAsync_WhenProductNotFound_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var item = new UpdateProductItem(productId, "Name", null, 10m);
        var batchRequest = new UpdateProductsRequest([item]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ProductsByIdsWithLinksSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        var sut = new UpdateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _productDataLinkRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _updateValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new UpdateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.FailureCount.ShouldBe(1);
        result.Failures[0].Errors.ShouldContain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task BatchDeleteAsync_CallsRepositoryDelete()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Delete me",
            Price = 10m,
            ProductDataLinks =
            [
                new ProductDataLink { ProductId = Guid.NewGuid(), ProductDataId = Guid.NewGuid() },
            ],
        };

        _repositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ProductsByIdsWithLinksSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([product]);

        var batchRequest = new BatchDeleteRequest([product.Id]);

        var sut = new DeleteProductsCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object
        );
        var result = await sut.HandleAsync(
            new DeleteProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        result.FailureCount.ShouldBe(0);
        result.Failures.ShouldBeEmpty();

        _repositoryMock.Verify(
            r =>
                r.DeleteRangeAsync(
                    It.Is<IEnumerable<Product>>(p => p.Contains(product)),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TransactionOptions?>()
                ),
            Times.Once
        );
        product.ProductDataLinks.ShouldBeEmpty();
    }

    [Fact]
    public async Task BatchUpdateAsync_WhenProductExists_UpdatesFields()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Description = "Old Desc",
            Price = 10m,
            Audit = new() { CreatedAtUtc = DateTime.UtcNow },
            ProductDataLinks = [],
        };

        var item = new UpdateProductItem(product.Id, "New Name", "New Desc", 20m);
        var batchRequest = new UpdateProductsRequest([item]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ProductsByIdsWithLinksSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([product]);

        var sut = new UpdateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _productDataLinkRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _updateValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new UpdateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        result.Failures.ShouldBeEmpty();

        product.Name.ShouldBe("New Name");
        product.Description.ShouldBe("New Desc");
        product.Price.ShouldBe(20m);

        _repositoryMock.Verify(
            r => r.UpdateAsync(product, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TransactionOptions?>()
                ),
            Times.Once
        );
        _productDataLinkRepositoryMock.Verify(
            r =>
                r.ListByProductIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task BatchUpdateAsync_ReplacesProductDataLinks()
    {
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Price = 10m,
            ProductDataLinks =
            [
                new ProductDataLink { ProductId = Guid.NewGuid(), ProductDataId = oldId },
            ],
        };
        var item = new UpdateProductItem(product.Id, "New Name", "New Desc", 20m, null, [newId]);
        var batchRequest = new UpdateProductsRequest([item]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ProductsByIdsWithLinksSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([product]);
        _productDataLinkRepositoryMock
            .Setup(r => r.ListByProductIdAsync(product.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product.ProductDataLinks.ToList());
        _productDataRepositoryMock
            .Setup(r =>
                r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([new ImageProductData { Id = newId, Title = "Image" }]);

        var sut = new UpdateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _productDataLinkRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _updateValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new UpdateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        product.ProductDataLinks.Select(x => x.ProductDataId).ShouldBe([newId]);
    }

    [Fact]
    public async Task BatchUpdateAsync_WithEmptyProductDataIds_RemovesExistingLinks()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Price = 10m,
            ProductDataLinks =
            [
                new ProductDataLink { ProductId = Guid.NewGuid(), ProductDataId = Guid.NewGuid() },
            ],
        };
        var item = new UpdateProductItem(product.Id, "New Name", "New Desc", 20m, null, []);
        var batchRequest = new UpdateProductsRequest([item]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ProductsByIdsWithLinksSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([product]);
        _productDataLinkRepositoryMock
            .Setup(r => r.ListByProductIdAsync(product.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product.ProductDataLinks.ToList());

        var sut = new UpdateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _productDataLinkRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _updateValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new UpdateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        product.ProductDataLinks.ShouldBeEmpty();
    }

    [Fact]
    public async Task BatchUpdateAsync_WithNullProductDataIds_KeepsExistingLinks()
    {
        var existingId = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Price = 10m,
            ProductDataLinks =
            [
                new ProductDataLink { ProductId = Guid.NewGuid(), ProductDataId = existingId },
            ],
        };
        var item = new UpdateProductItem(product.Id, "New Name", "New Desc", 20m, null, null);
        var batchRequest = new UpdateProductsRequest([item]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ProductsByIdsWithLinksSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([product]);

        var sut = new UpdateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _productDataLinkRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _updateValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new UpdateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        product.ProductDataLinks.Select(x => x.ProductDataId).ShouldBe([existingId]);
        _productDataRepositoryMock.Verify(
            r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _productDataLinkRepositoryMock.Verify(
            r =>
                r.ListByProductIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task BatchUpdateAsync_RestoresSoftDeletedProductDataLink()
    {
        var restoredId = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Old Name",
            Price = 10m,
            ProductDataLinks = [],
        };
        var deletedLink = ProductDataLink.Create(product.Id, restoredId);
        deletedLink.IsDeleted = true;
        deletedLink.DeletedAtUtc = DateTime.UtcNow;
        deletedLink.DeletedBy = Guid.NewGuid();

        var item = new UpdateProductItem(product.Id, "New Name", null, 20m, null, [restoredId]);
        var batchRequest = new UpdateProductsRequest([item]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ProductsByIdsWithLinksSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([product]);
        _productDataLinkRepositoryMock
            .Setup(r => r.ListByProductIdAsync(product.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([deletedLink]);
        _productDataRepositoryMock
            .Setup(r =>
                r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([new ImageProductData { Id = restoredId, Title = "Image" }]);

        var sut = new UpdateProductsCommandHandler(
            _repositoryMock.Object,
            _categoryRepositoryMock.Object,
            _productDataRepositoryMock.Object,
            _productDataLinkRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _updateValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new UpdateProductsCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        product.ProductDataLinks.Select(x => x.ProductDataId).ShouldBe([restoredId]);
        product.ProductDataLinks.Single().IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        var ct = TestContext.Current.CancellationToken;
        var filter = new ProductFilter();
        IReadOnlyList<ProductResponse> items =
        [
            new ProductResponse(
                Guid.NewGuid(),
                "Product 1",
                null,
                10m,
                Guid.NewGuid(),
                DateTime.UtcNow,
                []
            ),
            new ProductResponse(
                Guid.NewGuid(),
                "Product 2",
                null,
                20m,
                Guid.NewGuid(),
                DateTime.UtcNow,
                []
            ),
        ];

        var paged = new PagedResponse<ProductResponse>(
            items,
            2,
            filter.PageNumber,
            filter.PageSize
        );
        _repositoryMock
            .Setup(r => r.GetPagedAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);
        _repositoryMock
            .Setup(r => r.GetCategoryFacetsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repositoryMock
            .Setup(r => r.GetPriceFacetsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new GetProductsQueryHandler(_repositoryMock.Object);
        var result = await sut.HandleAsync(new GetProductsQuery(filter), ct);

        result.Page.Items.Count().ShouldBe(2);
        result.Page.TotalCount.ShouldBe(2);
    }
}
