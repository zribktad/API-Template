using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Category;
using APITemplate.Application.Features.Category.Mappings;
using APITemplate.Application.Features.Category.Specifications;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Domain.Options;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Handlers;

public class CategoryRequestHandlersTests
{
    private readonly Mock<ICategoryRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEventPublisher> _publisherMock;
    private readonly Mock<IValidator<CreateCategoryRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateCategoryItem>> _updateValidatorMock;

    public CategoryRequestHandlersTests()
    {
        _repositoryMock = new Mock<ICategoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publisherMock = new Mock<IEventPublisher>();
        _createValidatorMock = new Mock<IValidator<CreateCategoryRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateCategoryItem>>();
        _unitOfWorkMock.SetupImmediateTransactionExecution();

        _createValidatorMock
            .Setup(v =>
                v.ValidateAsync(It.IsAny<CreateCategoryRequest>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ValidationResult());

        _updateValidatorMock
            .Setup(v =>
                v.ValidateAsync(It.IsAny<UpdateCategoryItem>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ValidationResult());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedCategories()
    {
        var ct = TestContext.Current.CancellationToken;
        var items = new List<CategoryResponse>
        {
            new(Guid.NewGuid(), "Electronics", null, DateTime.UtcNow),
            new(Guid.NewGuid(), "Books", "All books", DateTime.UtcNow),
        };

        var paged = new PagedResponse<CategoryResponse>(items, 2, 1, 10);
        _repositoryMock
            .Setup(r =>
                r.GetPagedAsync(
                    It.IsAny<CategorySpecification>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(paged);

        var sut = new GetCategoriesQueryHandler(_repositoryMock.Object);
        var result = await sut.HandleAsync(new GetCategoriesQuery(new CategoryFilter()), ct);

        result.Items.Count().ShouldBe(2);
        result.Items.First().Name.ShouldBe("Electronics");
        result.Items.Last().Name.ShouldBe("Books");
        result.Items.Last().Description.ShouldBe("All books");
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var ct = TestContext.Current.CancellationToken;
        var paged = new PagedResponse<CategoryResponse>(new List<CategoryResponse>(), 0, 1, 10);
        _repositoryMock
            .Setup(r =>
                r.GetPagedAsync(
                    It.IsAny<CategorySpecification>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(paged);

        var sut = new GetCategoriesQueryHandler(_repositoryMock.Object);
        var result = await sut.HandleAsync(new GetCategoriesQuery(new CategoryFilter()), ct);

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryExists_ReturnsResponse()
    {
        var ct = TestContext.Current.CancellationToken;
        var categoryId = Guid.NewGuid();
        var response = new CategoryResponse(
            categoryId,
            "Electronics",
            "Electronic devices",
            DateTime.UtcNow
        );

        _repositoryMock
            .Setup(r =>
                r.FirstOrDefaultAsync(
                    It.IsAny<CategoryByIdSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(response);

        var sut = new GetCategoryByIdQueryHandler(_repositoryMock.Object);
        var result = await sut.HandleAsync(new GetCategoryByIdQuery(categoryId), ct);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(categoryId);
        result.Name.ShouldBe("Electronics");
        result.Description.ShouldBe("Electronic devices");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        _repositoryMock
            .Setup(r =>
                r.FirstOrDefaultAsync(
                    It.IsAny<CategoryByIdSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((CategoryResponse?)null);

        var sut = new GetCategoryByIdQueryHandler(_repositoryMock.Object);
        var result = await sut.HandleAsync(new GetCategoryByIdQuery(Guid.NewGuid()), ct);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task BatchCreateAsync_CreatesAndReturnsBatchResponse()
    {
        var request = new CreateCategoryRequest("Electronics", "Electronic devices");
        var batchRequest = new CreateCategoriesRequest([request]);

        _repositoryMock
            .Setup(r =>
                r.AddRangeAsync(It.IsAny<IEnumerable<Category>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                (IEnumerable<Category> entities, CancellationToken _) => entities.ToList()
            );

        var sut = new CreateCategoriesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _createValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateCategoriesCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.ShouldNotBeNull();
        result.SuccessCount.ShouldBe(1);
        result.FailureCount.ShouldBe(0);
        result.Results.Count.ShouldBe(1);
        result.Results[0].Success.ShouldBeTrue();
        result.Results[0].Id.ShouldNotBeNull();
        result.Results[0].Id!.Value.ShouldNotBe(Guid.Empty);

        _repositoryMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<Category>>(), It.IsAny<CancellationToken>()),
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
    public async Task BatchCreateAsync_WithNullDescription_CreatesCategory()
    {
        var request = new CreateCategoryRequest("Books", null);
        var batchRequest = new CreateCategoriesRequest([request]);

        _repositoryMock
            .Setup(r =>
                r.AddRangeAsync(It.IsAny<IEnumerable<Category>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                (IEnumerable<Category> entities, CancellationToken _) => entities.ToList()
            );

        var sut = new CreateCategoriesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _createValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateCategoriesCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        result.FailureCount.ShouldBe(0);
        result.Results[0].Success.ShouldBeTrue();
    }

    [Fact]
    public async Task BatchCreateAsync_WithValidationFailure_ReturnsFailureResponse()
    {
        var request = new CreateCategoryRequest("", null);
        var batchRequest = new CreateCategoriesRequest([request]);

        _createValidatorMock
            .Setup(v =>
                v.ValidateAsync(It.IsAny<CreateCategoryRequest>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new ValidationResult([new ValidationFailure("Name", "Category name is required.")])
            );

        var sut = new CreateCategoriesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _createValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateCategoriesCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(0);
        result.FailureCount.ShouldBe(1);
        result.Results[0].Success.ShouldBeFalse();
        result.Results[0].Errors.ShouldNotBeNull();
        result.Results[0].Errors!.ShouldContain("Category name is required.");

        _repositoryMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<Category>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task BatchUpdateAsync_WhenCategoryExists_UpdatesAndCommits()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Description = "Old Description",
            Audit = new() { CreatedAtUtc = DateTime.UtcNow },
        };

        var updateItem = new UpdateCategoryItem(category.Id, "New Name", "New Description");
        var batchRequest = new UpdateCategoriesRequest([updateItem]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(It.IsAny<CategoriesByIdsSpecification>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([category]);

        var sut = new UpdateCategoriesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _updateValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new UpdateCategoriesCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        result.FailureCount.ShouldBe(0);
        result.Results[0].Success.ShouldBeTrue();

        _repositoryMock.Verify(
            r =>
                r.UpdateAsync(
                    It.Is<Category>(c =>
                        c.Name == "New Name" && c.Description == "New Description"
                    ),
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
    public async Task BatchUpdateAsync_WhenCategoryDoesNotExist_ReturnsFailure()
    {
        var nonExistentId = Guid.NewGuid();
        var updateItem = new UpdateCategoryItem(nonExistentId, "Name", null);
        var batchRequest = new UpdateCategoriesRequest([updateItem]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(It.IsAny<CategoriesByIdsSpecification>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new List<Category>());

        var sut = new UpdateCategoriesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _updateValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new UpdateCategoriesCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(0);
        result.FailureCount.ShouldBe(1);
        result.Results[0].Success.ShouldBeFalse();
        result.Results[0].Errors.ShouldNotBeNull();
        result.Results[0].Errors!.ShouldContain(e => e.Contains("not found"));

        _unitOfWorkMock.Verify(
            u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TransactionOptions?>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task BatchUpdateAsync_WithValidationFailure_ReturnsFailureWithoutLoadingCategories()
    {
        var updateItem = new UpdateCategoryItem(Guid.NewGuid(), "", null);
        var batchRequest = new UpdateCategoriesRequest([updateItem]);

        _updateValidatorMock
            .Setup(v =>
                v.ValidateAsync(It.IsAny<UpdateCategoryItem>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new ValidationResult([new ValidationFailure("Name", "Category name is required.")])
            );

        var sut = new UpdateCategoriesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object,
            _updateValidatorMock.Object
        );
        var result = await sut.HandleAsync(
            new UpdateCategoriesCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(0);
        result.FailureCount.ShouldBe(1);
        result.Results[0].Success.ShouldBeFalse();
        result.Results[0].Errors.ShouldNotBeNull();
        result.Results[0].Errors!.ShouldContain("Category name is required.");

        _repositoryMock.Verify(
            r =>
                r.ListAsync(
                    It.IsAny<CategoriesByIdsSpecification>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task BatchDeleteAsync_WhenCategoryExists_DeletesAndCommits()
    {
        var id = Guid.NewGuid();
        var category = new Category { Id = id, Name = "To Delete" };
        var batchRequest = new BatchDeleteRequest([id]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(It.IsAny<CategoriesByIdsSpecification>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([category]);

        var sut = new DeleteCategoriesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object
        );
        var result = await sut.HandleAsync(
            new DeleteCategoriesCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(1);
        result.FailureCount.ShouldBe(0);
        result.Results[0].Success.ShouldBeTrue();
        result.Results[0].Id.ShouldBe(id);

        _repositoryMock.Verify(
            r => r.DeleteAsync(It.Is<Category>(c => c.Id == id), It.IsAny<CancellationToken>()),
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
    public async Task BatchDeleteAsync_WhenCategoryDoesNotExist_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        var batchRequest = new BatchDeleteRequest([id]);

        _repositoryMock
            .Setup(r =>
                r.ListAsync(It.IsAny<CategoriesByIdsSpecification>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new List<Category>());

        var sut = new DeleteCategoriesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object
        );
        var result = await sut.HandleAsync(
            new DeleteCategoriesCommand(batchRequest),
            TestContext.Current.CancellationToken
        );

        result.SuccessCount.ShouldBe(0);
        result.FailureCount.ShouldBe(1);
        result.Results[0].Success.ShouldBeFalse();
        result.Results[0].Errors.ShouldNotBeNull();
        result.Results[0].Errors!.ShouldContain(e => e.Contains("not found"));

        _unitOfWorkMock.Verify(
            u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TransactionOptions?>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task GetStatsAsync_WhenStatsExist_ReturnsMappedResponse()
    {
        var ct = TestContext.Current.CancellationToken;
        var categoryId = Guid.NewGuid();
        var stats = new ProductCategoryStats
        {
            CategoryId = categoryId,
            CategoryName = "Electronics",
            ProductCount = 5,
            AveragePrice = 199.99m,
            TotalReviews = 42,
        };

        _repositoryMock
            .Setup(r => r.GetStatsByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var sut = new GetCategoryStatsQueryHandler(_repositoryMock.Object);
        var result = await sut.HandleAsync(new GetCategoryStatsQuery(categoryId), ct);

        result.ShouldNotBeNull();
        result!.CategoryId.ShouldBe(categoryId);
        result.CategoryName.ShouldBe("Electronics");
        result.ProductCount.ShouldBe(5);
        result.AveragePrice.ShouldBe(199.99m);
        result.TotalReviews.ShouldBe(42);
    }

    [Fact]
    public async Task GetStatsAsync_WhenCategoryDoesNotExist_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        _repositoryMock
            .Setup(r => r.GetStatsByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategoryStats?)null);

        var sut = new GetCategoryStatsQueryHandler(_repositoryMock.Object);
        var result = await sut.HandleAsync(new GetCategoryStatsQuery(Guid.NewGuid()), ct);

        result.ShouldBeNull();
    }
}
