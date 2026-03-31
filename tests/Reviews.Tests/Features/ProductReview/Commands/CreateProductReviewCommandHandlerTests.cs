using Microsoft.EntityFrameworkCore;
using Moq;
using Reviews.Application.Common.Errors;
using Reviews.Application.Common.Responses;
using Reviews.Application.Features.CreateReview;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Interfaces;
using Shouldly;
using Wolverine;
using Xunit;
using ProductReviewEntity = Reviews.Domain.Entities.ProductReview;

namespace Reviews.Tests.Features.ProductReview.Commands;

public sealed class CreateProductReviewCommandHandlerTests
{
    private readonly Mock<IProductReviewRepository> _reviewRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IActorProvider> _actorProviderMock = new();
    private readonly Mock<IMessageBus> _busMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public CreateProductReviewCommandHandlerTests()
    {
        _actorProviderMock.Setup(a => a.ActorId).Returns(_userId);
        _unitOfWorkMock
            .Setup(u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task<ProductReviewEntity>>>(),
                    It.IsAny<CancellationToken>(),
                    null
                )
            )
            .Returns<Func<Task<ProductReviewEntity>>, CancellationToken, object?>(
                (action, _, _) => action()
            );
        _busMock
            .Setup(b => b.PublishAsync(It.IsAny<object>(), null))
            .Returns(ValueTask.CompletedTask);
    }

    [Fact]
    public async Task HandleAsync_WhenProductDoesNotExist_ReturnsNotFoundError()
    {
        using TestDbContext dbContext = CreateDbContext();
        Guid productId = Guid.NewGuid();
        CreateProductReviewRequest request = new(productId, "Great!", 5);
        CreateProductReviewCommand command = new(request);

        var (result, _) = await CreateProductReviewCommandHandler.HandleAsync(
            command,
            _reviewRepoMock.Object,
            dbContext,
            _unitOfWorkMock.Object,
            _actorProviderMock.Object,
            _busMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(ErrorCatalog.Reviews.ProductNotFoundForReview);
    }

    [Fact]
    public async Task HandleAsync_WhenProductExistsButInactive_ReturnsNotFoundError()
    {
        using TestDbContext dbContext = CreateDbContext();
        Guid productId = Guid.NewGuid();
        dbContext
            .Set<ProductProjection>()
            .Add(
                new ProductProjection
                {
                    ProductId = productId,
                    TenantId = Guid.NewGuid(),
                    Name = "Inactive Product",
                    IsActive = false,
                }
            );
        await dbContext.SaveChangesAsync();

        CreateProductReviewRequest request = new(productId, "Should fail", 3);
        CreateProductReviewCommand command = new(request);

        var (result, _) = await CreateProductReviewCommandHandler.HandleAsync(
            command,
            _reviewRepoMock.Object,
            dbContext,
            _unitOfWorkMock.Object,
            _actorProviderMock.Object,
            _busMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(ErrorCatalog.Reviews.ProductNotFoundForReview);
    }

    [Fact]
    public async Task HandleAsync_WhenProductExistsAndActive_CreatesReview()
    {
        using TestDbContext dbContext = CreateDbContext();
        Guid productId = Guid.NewGuid();
        dbContext
            .Set<ProductProjection>()
            .Add(
                new ProductProjection
                {
                    ProductId = productId,
                    TenantId = Guid.NewGuid(),
                    Name = "Active Product",
                    IsActive = true,
                }
            );
        await dbContext.SaveChangesAsync();

        CreateProductReviewRequest request = new(productId, "Excellent!", 5);
        CreateProductReviewCommand command = new(request);

        var (result, _) = await CreateProductReviewCommandHandler.HandleAsync(
            command,
            _reviewRepoMock.Object,
            dbContext,
            _unitOfWorkMock.Object,
            _actorProviderMock.Object,
            _busMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.ProductId.ShouldBe(productId);
        result.Value.Rating.ShouldBe(5);
        result.Value.UserId.ShouldBe(_userId);
    }

    private static TestDbContext CreateDbContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }
}
