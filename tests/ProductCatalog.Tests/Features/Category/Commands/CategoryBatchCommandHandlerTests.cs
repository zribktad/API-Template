using Ardalis.Specification;
using Moq;
using ProductCatalog.Application.Common.Errors;
using ProductCatalog.Application.Features.Category.Commands;
using ProductCatalog.Application.Features.Category.DTOs;
using ProductCatalog.Application.Features.Category.Validation;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Application.Batch.Rules;
using SharedKernel.Domain.Interfaces;
using Shouldly;
using Xunit;
using CategoryEntity = ProductCatalog.Domain.Entities.Category;

namespace ProductCatalog.Tests.Features.Category.Commands;

public sealed class CategoryBatchCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public CategoryBatchCommandHandlerTests()
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
    }

    [Fact]
    public async Task CreateHandleAsync_WhenValidationFails_ReturnsBatchFailureAndSkipsPersistence()
    {
        CreateCategoriesCommand command = new(new CreateCategoriesRequest([new("", null)]));

        var (result, _) = await CreateCategoriesCommandHandler.HandleAsync(
            command,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            CreateBatchRule<CreateCategoryRequest>(new CreateCategoryRequestValidator()),
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.SuccessCount.ShouldBe(0);
        result.Value.FailureCount.ShouldBe(1);
        result.Value.Failures.ShouldHaveSingleItem();
        result.Value.Failures[0].Errors.ShouldContain("Category name is required.");
        _repositoryMock.Verify(
            r =>
                r.AddRangeAsync(
                    It.IsAny<IEnumerable<CategoryEntity>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateHandleAsync_WhenItemsAreValid_PersistsAllCategories()
    {
        CreateCategoryRequest first = new("Electronics", "Devices");
        CreateCategoryRequest second = new("Books", null);
        CreateCategoriesCommand command = new(new CreateCategoriesRequest([first, second]));
        List<ProductCatalog.Domain.Entities.Category>? persistedCategories = null;

        _repositoryMock
            .Setup(r =>
                r.AddRangeAsync(
                    It.IsAny<IEnumerable<CategoryEntity>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<IEnumerable<CategoryEntity>, CancellationToken>(
                (categories, _) => persistedCategories = categories.ToList()
            )
            .ReturnsAsync([]);

        var (result, _) = await CreateCategoriesCommandHandler.HandleAsync(
            command,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            CreateBatchRule<CreateCategoryRequest>(new CreateCategoryRequestValidator()),
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.SuccessCount.ShouldBe(2);
        result.Value.FailureCount.ShouldBe(0);
        persistedCategories.ShouldNotBeNull();
        persistedCategories.Select(c => c.Name).ShouldBe(["Electronics", "Books"]);
    }

    [Fact]
    public async Task LoadAsync_WhenCategoryIsMissing_ReturnsStopWithoutLookup()
    {
        Guid missingId = Guid.NewGuid();
        UpdateCategoriesCommand command = new(
            new UpdateCategoriesRequest([new UpdateCategoryItem(missingId, "Updated", "Desc")])
        );

        _repositoryMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ISpecification<ProductCatalog.Domain.Entities.Category>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        var (continuation, lookup, _) = await UpdateCategoriesCommandHandler.LoadAsync(
            command,
            _repositoryMock.Object,
            CreateBatchRule<UpdateCategoryItem>(new UpdateCategoryItemValidator()),
            CancellationToken.None
        );

        continuation.ShouldBe(Wolverine.HandlerContinuation.Stop);
        lookup.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenLookupContainsEntities_UpdatesEachCategory()
    {
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();
        CategoryEntity first = new()
        {
            Id = firstId,
            Name = "Old 1",
            Description = "Old",
        };
        CategoryEntity second = new()
        {
            Id = secondId,
            Name = "Old 2",
            Description = "Old",
        };

        UpdateCategoriesCommand command = new(
            new UpdateCategoriesRequest([
                new UpdateCategoryItem(firstId, "New 1", "Desc 1"),
                new UpdateCategoryItem(secondId, "New 2", null),
            ])
        );

        var (result, _) = await UpdateCategoriesCommandHandler.HandleAsync(
            command,
            new SharedKernel.Application.Batch.EntityLookup<CategoryEntity>(
                new Dictionary<Guid, CategoryEntity> { [firstId] = first, [secondId] = second }
            ),
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.SuccessCount.ShouldBe(2);
        first.Name.ShouldBe("New 1");
        first.Description.ShouldBe("Desc 1");
        second.Name.ShouldBe("New 2");
        second.Description.ShouldBeNull();
        _repositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<CategoryEntity>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    private static FluentValidationBatchRule<T> CreateBatchRule<T>(
        FluentValidation.IValidator<T> validator
    )
    {
        Mock<IValidationMetrics> metrics = new();
        return new FluentValidationBatchRule<T>(validator, metrics.Object);
    }
}
