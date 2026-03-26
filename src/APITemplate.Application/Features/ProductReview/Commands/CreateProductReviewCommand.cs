using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.ProductReview.Mappings;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;
using ProductReviewEntity = APITemplate.Domain.Entities.ProductReview;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Creates a product review attributed to the authenticated user.</summary>
public sealed record CreateProductReviewCommand(CreateProductReviewRequest Request);

public sealed class CreateProductReviewCommandHandler
{
    public static async Task<(HandlerContinuation, OutgoingMessages)> LoadAsync(
        CreateProductReviewCommand command,
        IProductRepository productRepository,
        CancellationToken ct
    )
    {
        var productResult = await productRepository.GetByIdOrError(
            command.Request.ProductId,
            DomainErrors.Reviews.ProductNotFoundForReview(command.Request.ProductId),
            ct
        );

        OutgoingMessages messages = new();

        if (productResult.IsError)
        {
            messages.RespondToSender((ErrorOr<ProductReviewResponse>)productResult.Errors);
            return (HandlerContinuation.Stop, messages);
        }

        return (HandlerContinuation.Continue, messages);
    }

    public static async Task<(ErrorOr<ProductReviewResponse>, OutgoingMessages)> HandleAsync(
        CreateProductReviewCommand command,
        IProductReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IActorProvider actorProvider,
        CancellationToken ct
    )
    {
        var userId = actorProvider.ActorId;

        var review = await unitOfWork.ExecuteInTransactionAsync(
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

                await reviewRepository.AddAsync(entity, ct);
                return entity;
            },
            ct
        );

        return (review.ToResponse(), [new CacheInvalidationNotification(CacheTags.Reviews)]);
    }
}
