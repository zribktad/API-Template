using APITemplate.Application.Features.ProductReview.Mediator;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Mediator;

public class ProductReviewHandlersTests
{
    [Fact]
    public async Task CreateProductReviewCommandHandler_WhenProductNotFound_Throws()
    {
        var reviewRepoMock = new Mock<IProductReviewRepository>();
        var productRepoMock = new Mock<IProductRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        productRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var sut = new CreateProductReviewCommandHandler(reviewRepoMock.Object, productRepoMock.Object, uowMock.Object);

        var act = () => sut.Handle(
            new CreateProductReviewCommand(new CreateProductReviewRequest(Guid.NewGuid(), "Alice", null, 5)),
            CancellationToken.None);

        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task CreateProductReviewCommandHandler_WhenProductExists_CreatesAndCommits()
    {
        var reviewRepoMock = new Mock<IProductReviewRepository>();
        var productRepoMock = new Mock<IProductRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        var productId = Guid.NewGuid();
        productRepoMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = productId, Name = "P", Price = 1, CreatedAt = DateTime.UtcNow });

        var sut = new CreateProductReviewCommandHandler(reviewRepoMock.Object, productRepoMock.Object, uowMock.Object);

        var response = await sut.Handle(
            new CreateProductReviewCommand(new CreateProductReviewRequest(productId, "Alice", "Great", 5)),
            CancellationToken.None);

        response.ProductId.ShouldBe(productId);
        reviewRepoMock.Verify(r => r.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
