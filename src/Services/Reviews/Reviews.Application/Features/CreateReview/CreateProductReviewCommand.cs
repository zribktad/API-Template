using System.ComponentModel.DataAnnotations;
using Contracts.IntegrationEvents.Reviews;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Reviews.Application.Common.Events;
using Reviews.Application.Common.Mappings;
using Reviews.Application.Common.Responses;
using Reviews.Application.Common.Errors;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using SharedKernel.Application.Context;
using SharedKernel.Application.Validation;
using SharedKernel.Domain.Interfaces;
using Wolverine;
using ProductReviewEntity = Reviews.Domain.Entities.ProductReview;

namespace Reviews.Application.Features.CreateReview;

/// <summary>Payload for submitting a new product review, including the target product, an optional comment, and a 1-5 star rating.</summary>
public sealed record CreateProductReviewRequest(
    [NotEmpty(ErrorMessage = "ProductId is required.")] Guid ProductId,
    string? Comment,
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")] int Rating
);

/// <summary>Creates a new product review for the authenticated user and returns the persisted representation.</summary>
public sealed record CreateProductReviewCommand(CreateProductReviewRequest Request);

/// <summary>Handles <see cref="CreateProductReviewCommand"/>.</summary>
public sealed class CreateProductReviewCommandHandler
{
    public static async Task<ErrorOr<ProductReviewResponse>> HandleAsync(
        CreateProductReviewCommand command,
        IProductReviewRepository reviewRepository,
        DbContext dbContext,
        IUnitOfWork unitOfWork,
        IActorProvider actorProvider,
        IMessageBus bus,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        Guid userId = actorProvider.ActorId;

        bool productExists = await dbContext
            .Set<ProductProjection>()
            .AnyAsync(p => p.ProductId == command.Request.ProductId && p.IsActive, ct);

        if (!productExists)
            return DomainErrors.Reviews.ProductNotFoundForReview(command.Request.ProductId);

        ProductReviewEntity review = await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                ProductReviewEntity entity = new()
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

        await bus.PublishAsync(
            new ReviewCreatedIntegrationEvent(
                review.Id,
                review.ProductId,
                review.UserId,
                review.TenantId,
                review.Rating,
                timeProvider.GetUtcNow().UtcDateTime
            )
        );

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.Reviews));
        return review.ToResponse();
    }
}
