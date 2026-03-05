using APITemplate.Application.Features.Product.Mediator;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Mediator;

public class ProductHandlersTests
{
    [Fact]
    public async Task CreateProductCommandHandler_CreatesAndCommits()
    {
        var repoMock = new Mock<IProductRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var sut = new CreateProductCommandHandler(repoMock.Object, uowMock.Object);

        var request = new CreateProductRequest("Name", "Desc", 11m);

        var response = await sut.Handle(new CreateProductCommand(request), CancellationToken.None);

        response.Name.ShouldBe("Name");
        response.Price.ShouldBe(11m);
        repoMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WhenNotFound_Throws()
    {
        var repoMock = new Mock<IProductRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var sut = new UpdateProductCommandHandler(repoMock.Object, uowMock.Object);

        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var act = () => sut.Handle(
            new UpdateProductCommand(Guid.NewGuid(), new UpdateProductRequest("N", null, 1m)),
            CancellationToken.None);

        await Should.ThrowAsync<NotFoundException>(act);
    }
}
