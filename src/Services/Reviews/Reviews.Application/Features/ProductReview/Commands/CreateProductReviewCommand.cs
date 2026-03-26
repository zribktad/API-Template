using Contracts.IntegrationEvents.Reviews;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Reviews.Application.Common.Errors;
using Reviews.Application.Common.Events;
using Reviews.Application.Features.ProductReview.DTOs;
using Reviews.Application.Features.ProductReview.Mappings;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Interfaces;
using Wolverine;
using ProductReviewEntity = Reviews.Domain.Entities.ProductReview;

namespace Reviews.Application.Features.ProductReview.Commands;

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
                DateTime.UtcNow
            )
        );

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.Reviews));
        return review.ToResponse();
    }
}
