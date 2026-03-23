using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Wolverine;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Deletes the product review with the given identifier; only the review's author may delete it.</summary>
public sealed record DeleteProductReviewCommand(Guid Id) : IHasId;

/// <summary>Handles <see cref="DeleteProductReviewCommand"/>.</summary>
public sealed class DeleteProductReviewCommandHandler
{
    public static async Task HandleAsync(
        DeleteProductReviewCommand command,
        IProductReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IActorProvider actorProvider,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var userId = actorProvider.ActorId;
        var review = await reviewRepository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Reviews.ReviewNotFound,
            ct
        );

        if (review.UserId != userId)
        {
            throw new ForbiddenException(
                ErrorCatalog.Auth.ForbiddenOwnReviewsOnly,
                ErrorCatalog.Auth.Forbidden
            );
        }

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await reviewRepository.DeleteAsync(
                    command.Id,
                    ct,
                    ErrorCatalog.Reviews.ReviewNotFound
                );
            },
            ct
        );

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.Reviews));
    }
}
