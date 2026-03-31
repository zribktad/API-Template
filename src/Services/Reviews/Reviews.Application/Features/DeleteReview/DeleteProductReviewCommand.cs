using ErrorOr;
using Reviews.Application.Common.Errors;
using Reviews.Domain.Interfaces;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Context;
using SharedKernel.Application.Extensions;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Domain.Interfaces;
using Wolverine;

namespace Reviews.Application.Features.DeleteReview;

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
        Guid userId = actorProvider.ActorId;
        ErrorOr<Domain.Entities.ProductReview> reviewResult = await reviewRepository.GetByIdOrError(
            command.Id,
            DomainErrors.Reviews.NotFound(command.Id),
            ct
        );
        if (reviewResult.IsError)
            return (reviewResult.Errors, CacheInvalidationCascades.None);
        Domain.Entities.ProductReview review = reviewResult.Value;

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
