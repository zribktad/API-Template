using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;
using ProductReviewEntity = APITemplate.Domain.Entities.ProductReview;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Deletes the product review with the given identifier; only the review's author may delete it.</summary>
public sealed record DeleteProductReviewCommand(Guid Id) : IHasId;

/// <summary>Handles <see cref="DeleteProductReviewCommand"/>.</summary>
public sealed class DeleteProductReviewCommandHandler
{
    public static async Task<(
        HandlerContinuation,
        ProductReviewEntity?,
        OutgoingMessages
    )> LoadAsync(
        DeleteProductReviewCommand command,
        IProductReviewRepository reviewRepository,
        IActorProvider actorProvider,
        CancellationToken ct
    )
    {
        var userId = actorProvider.ActorId;
        var reviewResult = await reviewRepository.GetByIdOrError(
            command.Id,
            DomainErrors.Reviews.NotFound(command.Id),
            ct
        );

        OutgoingMessages messages = new();

        if (reviewResult.IsError)
        {
            messages.RespondToSender((ErrorOr<Success>)reviewResult.Errors);
            return (HandlerContinuation.Stop, null, messages);
        }

        var review = reviewResult.Value;

        if (review.UserId != userId)
        {
            messages.RespondToSender((ErrorOr<Success>)DomainErrors.Auth.ForbiddenOwnReviewsOnly());
            return (HandlerContinuation.Stop, null, messages);
        }

        return (HandlerContinuation.Continue, review, messages);
    }

    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        DeleteProductReviewCommand command,
        ProductReviewEntity review,
        IProductReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await reviewRepository.DeleteAsync(review, ct);
            },
            ct
        );

        return (Result.Success, [new CacheInvalidationNotification(CacheTags.Reviews)]);
    }
}
