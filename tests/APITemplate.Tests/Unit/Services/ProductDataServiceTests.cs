using APITemplate.Application.Features.ProductData.Mediator;
using APITemplate.Application.Features.ProductData.Services;
using MediatR;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Services;

public class ProductDataServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ProductDataService _sut;

    public ProductDataServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new ProductDataService(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_SendsGetProductDataQuery()
    {
        List<ProductDataResponse> expected = [];

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetProductDataQuery>(q => q.Type == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync();

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task GetByIdAsync_SendsGetProductDataByIdQuery()
    {
        var id = "507f1f77bcf86cd799439011";
        var expected = new ProductDataResponse
        {
            Id = id,
            Type = "image",
            Title = "Title",
            Width = 100,
            Height = 100,
            Format = "jpg",
            FileSizeBytes = 12
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetProductDataByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(id);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task CreateImageAsync_SendsCreateImageProductDataCommand()
    {
        var request = new CreateImageProductDataRequest("Title", null, 100, 100, "jpg", 12);
        var expected = new ProductDataResponse
        {
            Id = "id",
            Type = "image",
            Title = "Title",
            Width = 100,
            Height = 100,
            Format = "jpg",
            FileSizeBytes = 12
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<CreateImageProductDataCommand>(c => c.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.CreateImageAsync(request);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task CreateVideoAsync_SendsCreateVideoProductDataCommand()
    {
        var request = new CreateVideoProductDataRequest("Title", null, 30, "1080p", "mp4", 12);
        var expected = new ProductDataResponse
        {
            Id = "id",
            Type = "video",
            Title = "Title",
            DurationSeconds = 30,
            Resolution = "1080p",
            Format = "mp4",
            FileSizeBytes = 12
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<CreateVideoProductDataCommand>(c => c.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.CreateVideoAsync(request);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteProductDataCommand()
    {
        var id = "507f1f77bcf86cd799439011";

        _mediatorMock
            .Setup(m => m.Send(It.Is<DeleteProductDataCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.DeleteAsync(id);

        _mediatorMock.Verify(
            m => m.Send(It.Is<DeleteProductDataCommand>(c => c.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
