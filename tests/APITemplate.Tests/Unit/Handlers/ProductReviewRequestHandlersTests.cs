using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.ProductReview;
using APITemplate.Application.Features.ProductReview.Specifications;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using APITemplate.Domain.Options;
using Moq;
using Shouldly;
using Wolverine;
using Xunit;

namespace APITemplate.Tests.Unit.Handlers;

public class ProductReviewRequestHandlersTests
{
    private readonly Mock<IProductReviewRepository> _reviewRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IActorProvider> _actorProviderMock;
    private readonly Mock<IMessageBus> _busMock;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public ProductReviewRequestHandlersTests()
    {
        _reviewRepoMock = new Mock<IProductReviewRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _actorProviderMock = new Mock<IActorProvider>();
        _busMock = new Mock<IMessageBus>();
        _actorProviderMock.Setup(a => a.ActorId).Returns(_currentUserId);
        _unitOfWorkMock.SetupImmediateTransactionExecution();
        _unitOfWorkMock.SetupImmediateTransactionExecution<ProductReview>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllReviews()
    {
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var items = new List<ProductReviewResponse>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), userId, null, 5, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), userId, null, 3, DateTime.UtcNow),
        };

        var paged = new PagedResponse<ProductReviewResponse>(items, 2, 1, 10);
        _reviewRepoMock
            .Setup(r =>
                r.GetPagedAsync(
                    It.IsAny<ProductReviewSpecification>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(paged);

        var result = await GetProductReviewsQueryHandler.HandleAsync(
            new GetProductReviewsQuery(new ProductReviewFilter()),
            _reviewRepoMock.Object,
            ct
        );

        result.Items.Count().ShouldBe(2);
        result.TotalCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetByIdAsync_ReturnsExpectedResult(bool reviewExists)
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        ProductReview? entity = reviewExists
            ? new ProductReview
            {
                Id = reviewId,
                ProductId = Guid.NewGuid(),
                UserId = userId,
                Rating = 4,
                Audit = new() { CreatedAtUtc = DateTime.UtcNow },
            }
            : null;

        _reviewRepoMock
            .Setup(r => r.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await GetProductReviewByIdQueryHandler.HandleAsync(
            new GetProductReviewByIdQuery(reviewId),
            _reviewRepoMock.Object,
            ct
        );

        if (reviewExists)
        {
            result.ShouldNotBeNull();
            result!.UserId.ShouldBe(userId);
            result.Rating.ShouldBe(4);
        }
        else
        {
            result.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GetByProductIdAsync_ReturnsReviewsForProduct()
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var items = new List<ProductReviewResponse>
        {
            new(Guid.NewGuid(), productId, userId, null, 5, DateTime.UtcNow),
        };

        _reviewRepoMock
            .Setup(r =>
                r.ListAsync(
                    It.IsAny<ProductReviewByProductIdSpecification>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(items);

        var result = await GetProductReviewsByProductIdQueryHandler.HandleAsync(
            new GetProductReviewsByProductIdQuery(productId),
            _reviewRepoMock.Object,
            ct
        );

        result.Count.ShouldBe(1);
        result[0].ProductId.ShouldBe(productId);
    }

    [Fact]
    public async Task CreateAsync_WhenProductExists_CreatesReview()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Price = 10m,
            Audit = new() { CreatedAtUtc = DateTime.UtcNow },
        };
        var request = new CreateProductReviewRequest(product.Id, "Great!", 5);

        _productRepoMock
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _reviewRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview rv, CancellationToken _) => rv);

        var result = await CreateProductReviewCommandHandler.HandleAsync(
            new CreateProductReviewCommand(request),
            _reviewRepoMock.Object,
            _productRepoMock.Object,
            _unitOfWorkMock.Object,
            _actorProviderMock.Object,
            _busMock.Object,
            TestContext.Current.CancellationToken
        );

        result.UserId.ShouldBe(_currentUserId);
        result.Rating.ShouldBe(5);
        result.ProductId.ShouldBe(product.Id);
        result.Id.ShouldNotBe(Guid.Empty);

        _reviewRepoMock.Verify(
            r => r.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task<ProductReview>>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TransactionOptions?>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        _productRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var request = new CreateProductReviewRequest(Guid.NewGuid(), null, 3);

        var act = () =>
            CreateProductReviewCommandHandler.HandleAsync(
                new CreateProductReviewCommand(request),
                _reviewRepoMock.Object,
                _productRepoMock.Object,
                _unitOfWorkMock.Object,
                _actorProviderMock.Object,
                _busMock.Object,
                TestContext.Current.CancellationToken
            );

        await Should.ThrowAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task DeleteAsync_WhenOwner_CallsRepositoryDelete()
    {
        var id = Guid.NewGuid();
        var review = new ProductReview
        {
            Id = id,
            UserId = _currentUserId,
            Rating = 3,
        };

        _reviewRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        await DeleteProductReviewCommandHandler.HandleAsync(
            new DeleteProductReviewCommand(id),
            _reviewRepoMock.Object,
            _unitOfWorkMock.Object,
            _actorProviderMock.Object,
            _busMock.Object,
            TestContext.Current.CancellationToken
        );

        _reviewRepoMock.Verify(
            r => r.DeleteAsync(id, It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TransactionOptions?>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwner_ThrowsForbiddenException()
    {
        var id = Guid.NewGuid();
        var review = new ProductReview
        {
            Id = id,
            UserId = Guid.NewGuid(),
            Rating = 3,
        };

        _reviewRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var act = () =>
            DeleteProductReviewCommandHandler.HandleAsync(
                new DeleteProductReviewCommand(id),
                _reviewRepoMock.Object,
                _unitOfWorkMock.Object,
                _actorProviderMock.Object,
                _busMock.Object,
                TestContext.Current.CancellationToken
            );

        await Should.ThrowAsync<ForbiddenException>(act);
        _reviewRepoMock.Verify(
            r =>
                r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();

        _reviewRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        var act = () =>
            DeleteProductReviewCommandHandler.HandleAsync(
                new DeleteProductReviewCommand(id),
                _reviewRepoMock.Object,
                _unitOfWorkMock.Object,
                _actorProviderMock.Object,
                _busMock.Object,
                TestContext.Current.CancellationToken
            );

        await Should.ThrowAsync<NotFoundException>(act);
    }
}
