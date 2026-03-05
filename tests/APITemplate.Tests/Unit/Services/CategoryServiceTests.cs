using APITemplate.Application.Features.Category.Mediator;
using APITemplate.Application.Features.Category.Services;
using MediatR;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Services;

public class CategoryServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new CategoryService(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_SendsGetCategoriesQuery()
    {
        IReadOnlyList<CategoryResponse> expected = [new(Guid.NewGuid(), "Books", null, DateTime.UtcNow)];

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync();

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task GetByIdAsync_SendsGetCategoryByIdQuery()
    {
        var id = Guid.NewGuid();
        var expected = new CategoryResponse(id, "Books", null, DateTime.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetCategoryByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(id);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task CreateAsync_SendsCreateCategoryCommand()
    {
        var request = new CreateCategoryRequest("Books", "Desc");
        var expected = new CategoryResponse(Guid.NewGuid(), "Books", "Desc", DateTime.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.Is<CreateCategoryCommand>(c => c.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.CreateAsync(request);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task UpdateAsync_SendsUpdateCategoryCommand()
    {
        var id = Guid.NewGuid();
        var request = new UpdateCategoryRequest("Books", "Desc");

        _mediatorMock
            .Setup(m => m.Send(It.Is<UpdateCategoryCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.UpdateAsync(id, request);

        _mediatorMock.Verify(
            m => m.Send(It.Is<UpdateCategoryCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteCategoryCommand()
    {
        var id = Guid.NewGuid();

        _mediatorMock
            .Setup(m => m.Send(It.Is<DeleteCategoryCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.DeleteAsync(id);

        _mediatorMock.Verify(
            m => m.Send(It.Is<DeleteCategoryCommand>(c => c.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStatsAsync_SendsGetCategoryStatsQuery()
    {
        var id = Guid.NewGuid();
        var expected = new ProductCategoryStatsResponse(id, "Books", 3, 10m, 4);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetCategoryStatsQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetStatsAsync(id);

        result.ShouldBe(expected);
    }
}
