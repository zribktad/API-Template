using APITemplate.Application.Features.ProductData.Mediator;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Mediator;

public class ProductDataHandlersTests
{
    [Fact]
    public async Task CreateImageProductDataCommandHandler_CreatesImageDocument()
    {
        var repoMock = new Mock<IProductDataRepository>();
        repoMock
            .Setup(r => r.CreateAsync(It.IsAny<ImageProductData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductData d, CancellationToken _) => d);

        var sut = new CreateImageProductDataCommandHandler(repoMock.Object);
        var request = new CreateImageProductDataRequest("Banner", "Desc", 100, 200, "jpg", 1_000);

        var response = await sut.Handle(new CreateImageProductDataCommand(request), CancellationToken.None);

        response.Type.ShouldBe("image");
        response.Title.ShouldBe("Banner");
        repoMock.Verify(r => r.CreateAsync(It.IsAny<ImageProductData>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
