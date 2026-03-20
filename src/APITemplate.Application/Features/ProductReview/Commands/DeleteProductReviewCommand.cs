using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Deletes the product review with the given identifier; only the review's author may delete it.</summary>
public sealed record DeleteProductReviewCommand(Guid Id) : ICommand;

/// <summary>Handles <see cref="DeleteProductReviewCommand"/>.</summary>
public sealed class DeleteProductReviewCommandHandler : ICommandHandler<DeleteProductReviewCommand>
{
    private readonly IProductReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActorProvider _actorProvider;
    private readonly IEventPublisher _publisher;

    public DeleteProductReviewCommandHandler(
        IProductReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IActorProvider actorProvider,
        IEventPublisher publisher
    )
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _actorProvider = actorProvider;
        _publisher = publisher;
    }

    public async Task HandleAsync(DeleteProductReviewCommand command, CancellationToken ct)
    {
        var userId = _actorProvider.ActorId;
        var review = await _reviewRepository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Reviews.ReviewNotFound,
            ct
        );

        if (review.UserId != userId)
        {
            throw new ForbiddenException(
                "You can only delete your own reviews.",
                ErrorCatalog.Auth.Forbidden
            );
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _reviewRepository.DeleteAsync(
                    command.Id,
                    ct,
                    ErrorCatalog.Reviews.ReviewNotFound
                );
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Reviews), ct);
    }
}
