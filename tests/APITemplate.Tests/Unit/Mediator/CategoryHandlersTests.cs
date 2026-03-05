using APITemplate.Application.Features.Category.Mediator;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Mediator;

public class CategoryHandlersTests
{
    [Fact]
    public async Task CreateCategoryCommandHandler_CreatesAndCommits()
    {
        var repoMock = new Mock<ICategoryRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var sut = new CreateCategoryCommandHandler(repoMock.Object, uowMock.Object);

        var response = await sut.Handle(
            new CreateCategoryCommand(new CreateCategoryRequest("Books", "Desc")),
            CancellationToken.None);

        response.Name.ShouldBe("Books");
        repoMock.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryCommandHandler_WhenNotFound_Throws()
    {
        var repoMock = new Mock<ICategoryRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var sut = new UpdateCategoryCommandHandler(repoMock.Object, uowMock.Object);

        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var act = () => sut.Handle(
            new UpdateCategoryCommand(Guid.NewGuid(), new UpdateCategoryRequest("Books", null)),
            CancellationToken.None);

        await Should.ThrowAsync<NotFoundException>(act);
    }
}
