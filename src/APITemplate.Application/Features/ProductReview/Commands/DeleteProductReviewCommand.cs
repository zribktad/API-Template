using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using SharedKernel.Application.Common.Events;
using Wolverine;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Deletes the product review with the given identifier; only the review's author may delete it.</summary>
public sealed record DeleteProductReviewCommand(Guid Id) : IHasId;

/// <summary>Handles <see cref="DeleteProductReviewCommand"/>.</summary>
public sealed class DeleteProductReviewCommandHandler
{
    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        DeleteProductReviewCommand command,
        IProductReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
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
        if (reviewResult.IsError)
            return (reviewResult.Errors, CacheInvalidationCascades.None);
        var review = reviewResult.Value;

        if (review.UserId != userId)
            return (DomainErrors.Auth.ForbiddenOwnReviewsOnly(), CacheInvalidationCascades.None);

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await reviewRepository.DeleteAsync(review, ct);
            },
            ct
        );

        return (Result.Success, CacheInvalidationCascades.ForTag(CacheTags.Reviews));
    }
}
