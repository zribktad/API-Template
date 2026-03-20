using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.ProductReview.Mappings;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using ProductReviewEntity = APITemplate.Domain.Entities.ProductReview;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Creates a new product review for the authenticated user and returns the persisted representation.</summary>
public sealed record CreateProductReviewCommand(CreateProductReviewRequest Request)
    : ICommand<ProductReviewResponse>;

/// <summary>Handles <see cref="CreateProductReviewCommand"/>.</summary>
public sealed class CreateProductReviewCommandHandler
    : ICommandHandler<CreateProductReviewCommand, ProductReviewResponse>
{
    private readonly IProductReviewRepository _reviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActorProvider _actorProvider;
    private readonly IEventPublisher _publisher;

    public CreateProductReviewCommandHandler(
        IProductReviewRepository reviewRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IActorProvider actorProvider,
        IEventPublisher publisher
    )
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _actorProvider = actorProvider;
        _publisher = publisher;
    }

    public async Task<ProductReviewResponse> HandleAsync(
        CreateProductReviewCommand command,
        CancellationToken ct
    )
    {
        var userId = _actorProvider.ActorId;
        await _productRepository.GetByIdOrThrowAsync(
            command.Request.ProductId,
            ErrorCatalog.Reviews.ProductNotFoundForReview,
            ct
        );

        var review = await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                var entity = new ProductReviewEntity
                {
                    Id = Guid.NewGuid(),
                    ProductId = command.Request.ProductId,
                    UserId = userId,
                    Comment = command.Request.Comment,
                    Rating = command.Request.Rating,
                };

                await _reviewRepository.AddAsync(entity, ct);
                return entity;
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Reviews), ct);
        return review.ToResponse();
    }
}
