using APITemplate.Application.Features.ProductReview.Mediator;
using APITemplate.Application.Features.ProductReview.Services;
using MediatR;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Services;

public class ProductReviewServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ProductReviewService _sut;

    public ProductReviewServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new ProductReviewService(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_SendsGetProductReviewsQuery()
    {
        var filter = new ProductReviewFilter(PageNumber: 2, PageSize: 5);
        var expected = new PagedResponse<ProductReviewResponse>([], 0, 2, 5);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetProductReviewsQuery>(q => q.Filter == filter), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync(filter);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task GetByIdAsync_SendsGetProductReviewByIdQuery()
    {
        var id = Guid.NewGuid();
        var expected = new ProductReviewResponse(id, Guid.NewGuid(), "Alice", null, 4, DateTime.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetProductReviewByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(id);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task GetByProductIdAsync_SendsGetProductReviewsByProductIdQuery()
    {
        var productId = Guid.NewGuid();
        IReadOnlyList<ProductReviewResponse> expected = [new(Guid.NewGuid(), productId, "Alice", null, 5, DateTime.UtcNow)];

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetProductReviewsByProductIdQuery>(q => q.ProductId == productId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByProductIdAsync(productId);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task CreateAsync_SendsCreateProductReviewCommand()
    {
        var request = new CreateProductReviewRequest(Guid.NewGuid(), "Alice", "Great", 5);
        var expected = new ProductReviewResponse(Guid.NewGuid(), request.ProductId, "Alice", "Great", 5, DateTime.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.Is<CreateProductReviewCommand>(c => c.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.CreateAsync(request);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteProductReviewCommand()
    {
        var id = Guid.NewGuid();

        _mediatorMock
            .Setup(m => m.Send(It.Is<DeleteProductReviewCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.DeleteAsync(id);

        _mediatorMock.Verify(
            m => m.Send(It.Is<DeleteProductReviewCommand>(c => c.Id == id), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
