using APITemplate.Application.Features.Product.Mediator;
using APITemplate.Application.Features.Product.Services;
using MediatR;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Services;

public class ProductServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new ProductService(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_SendsGetProductByIdQuery()
    {
        var id = Guid.NewGuid();
        var expected = new ProductResponse(id, "Test", "Desc", 10m, DateTime.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetProductByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(id);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task GetAllAsync_SendsGetProductsQuery()
    {
        var filter = new ProductFilter(Name: "Phone");
        var expected = new PagedResponse<ProductResponse>([], 0, 1, 10);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetProductsQuery>(q => q.Filter == filter), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync(filter);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task CreateAsync_SendsCreateProductCommand()
    {
        var request = new CreateProductRequest("Name", "Desc", 20m);
        var expected = new ProductResponse(Guid.NewGuid(), "Name", "Desc", 20m, DateTime.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.Is<CreateProductCommand>(c => c.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.CreateAsync(request);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task UpdateAsync_SendsUpdateProductCommand()
    {
        var id = Guid.NewGuid();
        var request = new UpdateProductRequest("New", "Desc", 30m);

        _mediatorMock
            .Setup(m => m.Send(It.Is<UpdateProductCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.UpdateAsync(id, request);

        _mediatorMock.Verify(
            m => m.Send(It.Is<UpdateProductCommand>(c => c.Id == id && c.Request == request), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteProductCommand()
    {
        var id = Guid.NewGuid();

        _mediatorMock
            .Setup(m => m.Send(It.Is<DeleteProductCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.DeleteAsync(id);

        _mediatorMock.Verify(
            m => m.Send(It.Is<DeleteProductCommand>(c => c.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
