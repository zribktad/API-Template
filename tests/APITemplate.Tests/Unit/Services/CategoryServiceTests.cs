using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using APITemplate.Domain.Options;
using APITemplate.Application.Features.Category.Mappings;
using APITemplate.Application.Features.Category.Services;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repositoryMock;
    private readonly Mock<ICategoryQueryService> _queryServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _repositoryMock = new Mock<ICategoryRepository>();
        _queryServiceMock = new Mock<ICategoryQueryService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.SetupImmediateTransactionExecution();
        _unitOfWorkMock.SetupImmediateTransactionExecution<Category>();
        _sut = new CategoryService(_repositoryMock.Object, _queryServiceMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedCategories()
    {
        var ct = TestContext.Current.CancellationToken;
        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Electronics", TenantId = Guid.NewGuid(), Audit = new() { CreatedAtUtc = DateTime.UtcNow } },
            new() { Id = Guid.NewGuid(), Name = "Books", Description = "All books", TenantId = Guid.NewGuid(), Audit = new() { CreatedAtUtc = DateTime.UtcNow } }
        };
        var response = new PagedResponse<CategoryResponse>(categories.Select(x => x.ToResponse()).ToArray(), 2, 1, 10);

        _queryServiceMock
            .Setup(q => q.GetPagedAsync(It.IsAny<CategoryFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.GetAllAsync(new CategoryFilter(), ct);

        result.Items.Count().ShouldBe(2);
        result.Items.First().Name.ShouldBe("Electronics");
        result.Items.Last().Name.ShouldBe("Books");
        result.Items.Last().Description.ShouldBe("All books");
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var ct = TestContext.Current.CancellationToken;
        _queryServiceMock
            .Setup(q => q.GetPagedAsync(It.IsAny<CategoryFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<CategoryResponse>([], 0, 1, 10));

        var result = await _sut.GetAllAsync(new CategoryFilter(), ct);

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryExists_ReturnsResponse()
    {
        var ct = TestContext.Current.CancellationToken;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Electronics",
            Description = "Electronic devices",
            Audit = new() { CreatedAtUtc = DateTime.UtcNow }
        };

        _queryServiceMock
            .Setup(q => q.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category.ToResponse());

        var result = await _sut.GetByIdAsync(category.Id, ct);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(category.Id);
        result.Name.ShouldBe("Electronics");
        result.Description.ShouldBe("Electronic devices");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        _queryServiceMock
            .Setup(q => q.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryResponse?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), ct);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsCategoryResponse()
    {
        var request = new CreateCategoryRequest("Electronics", "Electronic devices");

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category c, CancellationToken _) => c);

        var result = await _sut.CreateAsync(request, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("Electronics");
        result.Description.ShouldBe("Electronic devices");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<Category>>>(), It.IsAny<CancellationToken>(), It.IsAny<TransactionOptions?>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullDescription_CreatesCategory()
    {
        var request = new CreateCategoryRequest("Books", null);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category c, CancellationToken _) => c);

        var result = await _sut.CreateAsync(request, TestContext.Current.CancellationToken);

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
            Audit = new() { CreatedAtUtc = DateTime.UtcNow }
        };

        var request = new UpdateCategoryRequest("New Name", "New Description");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        await _sut.UpdateAsync(category.Id, request, TestContext.Current.CancellationToken);

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Category>(c => c.Name == "New Name" && c.Description == "New Description"),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(
            u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>(), It.IsAny<TransactionOptions?>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryDoesNotExist_ThrowsNotFoundException()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var act = () => _sut.UpdateAsync(Guid.NewGuid(), new UpdateCategoryRequest("Name", null), TestContext.Current.CancellationToken);

        await Should.ThrowAsync<NotFoundException>(act);
        _unitOfWorkMock.Verify(
            u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>(), It.IsAny<TransactionOptions?>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepositoryDeleteAndCommits()
    {
        var id = Guid.NewGuid();

        await _sut.DeleteAsync(id, TestContext.Current.CancellationToken);

        _repositoryMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>(), It.IsAny<string?>()), Times.Once);
        _unitOfWorkMock.Verify(
            u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>(), It.IsAny<TransactionOptions?>()),
            Times.Once);
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
            TotalReviews = 42
        };

        _repositoryMock
            .Setup(r => r.GetStatsByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await _sut.GetStatsAsync(categoryId, ct);

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

        var result = await _sut.GetStatsAsync(Guid.NewGuid(), ct);

        result.ShouldBeNull();
    }
}
