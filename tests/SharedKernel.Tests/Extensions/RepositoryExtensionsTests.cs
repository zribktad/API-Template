using Ardalis.Specification;
using ErrorOr;
using Moq;
using SharedKernel.Application.Extensions;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Extensions;

public sealed class RepositoryExtensionsTests
{
    private readonly Mock<IRepositoryBase<TestEntity>> _repositoryMock = new();

    [Fact]
    public async Task GetByIdOrError_WhenEntityExists_ReturnsEntity()
    {
        Guid id = Guid.NewGuid();
        TestEntity entity = new() { Id = id };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        Error notFoundError = Error.NotFound("TEST-404", "Not found");

        ErrorOr<TestEntity> result = await _repositoryMock.Object.GetByIdOrError(id, notFoundError);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(entity);
    }

    [Fact]
    public async Task GetByIdOrError_WhenEntityDoesNotExist_ReturnsNotFoundError()
    {
        Guid id = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestEntity?)null);
        Error notFoundError = Error.NotFound("TEST-404", "Entity not found");

        ErrorOr<TestEntity> result = await _repositoryMock.Object.GetByIdOrError(id, notFoundError);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(notFoundError);
    }

    public sealed class TestEntity
    {
        public Guid Id { get; set; }
    }
}
