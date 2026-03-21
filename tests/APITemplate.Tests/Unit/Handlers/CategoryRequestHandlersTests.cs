using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Category;
using APITemplate.Application.Features.Category.Mappings;
using APITemplate.Application.Features.Category.Specifications;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using APITemplate.Domain.Options;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Handlers;

public class CategoryRequestHandlersTests
{
    private readonly Mock<ICategoryRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEventPublisher> _publisherMock;

    public CategoryRequestHandlersTests()
    {
        _repositoryMock = new Mock<ICategoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _publisherMock = new Mock<IEventPublisher>();
        _unitOfWorkMock.SetupImmediateTransactionExecution();
        _unitOfWorkMock.SetupImmediateTransactionExecution<Category>();
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
    public async Task CreateAsync_CreatesAndReturnsCategoryResponse()
    {
        var request = new CreateCategoryRequest("Electronics", "Electronic devices");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category c, CancellationToken _) => c);

        var sut = new CreateCategoryCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateCategoryCommand(request),
            TestContext.Current.CancellationToken
        );

        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("Electronics");
        result.Description.ShouldBe("Electronic devices");

        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task<Category>>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TransactionOptions?>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateAsync_WithNullDescription_CreatesCategory()
    {
        var request = new CreateCategoryRequest("Books", null);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category c, CancellationToken _) => c);

        var sut = new CreateCategoryCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object
        );
        var result = await sut.HandleAsync(
            new CreateCategoryCommand(request),
            TestContext.Current.CancellationToken
        );

        result.Name.ShouldBe("Books");
        result.Description.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryExists_UpdatesAndCommits()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Description = "Old Description",
            Audit = new() { CreatedAtUtc = DateTime.UtcNow },
        };

        var request = new UpdateCategoryRequest("New Name", "New Description");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var sut = new UpdateCategoryCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object
        );
        await sut.HandleAsync(
            new UpdateCategoryCommand(category.Id, request),
            TestContext.Current.CancellationToken
        );

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
    public async Task UpdateAsync_WhenCategoryDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var sut = new UpdateCategoryCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object
        );
        var act = () =>
            sut.HandleAsync(
                new UpdateCategoryCommand(Guid.NewGuid(), new UpdateCategoryRequest("Name", null)),
                TestContext.Current.CancellationToken
            );

        await Should.ThrowAsync<NotFoundException>(act);
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
    public async Task DeleteAsync_CallsRepositoryDeleteAndCommits()
    {
        var id = Guid.NewGuid();

        var sut = new DeleteCategoryCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _publisherMock.Object
        );
        await sut.HandleAsync(new DeleteCategoryCommand(id), TestContext.Current.CancellationToken);

        _repositoryMock.Verify(
            r => r.DeleteAsync(id, It.IsAny<CancellationToken>(), It.IsAny<string?>()),
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
