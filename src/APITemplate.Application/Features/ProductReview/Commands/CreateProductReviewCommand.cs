using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.ProductReview.Mappings;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using SharedKernel.Application.Common.Events;
using Wolverine;
using ProductReviewEntity = APITemplate.Domain.Entities.ProductReview;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Creates a new product review for the authenticated user and returns the persisted representation.</summary>
public sealed record CreateProductReviewCommand(CreateProductReviewRequest Request);

/// <summary>Handles <see cref="CreateProductReviewCommand"/>.</summary>
public sealed class CreateProductReviewCommandHandler
{
    public static async Task<(ErrorOr<ProductReviewResponse>, OutgoingMessages)> HandleAsync(
        CreateProductReviewCommand command,
        IProductReviewRepository reviewRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IActorProvider actorProvider,
        CancellationToken ct
    )
    {
        var userId = actorProvider.ActorId;
        var productResult = await productRepository.GetByIdOrError(
            command.Request.ProductId,
            DomainErrors.Reviews.ProductNotFoundForReview(command.Request.ProductId),
            ct
        );
        if (productResult.IsError)
            return (productResult.Errors, CacheInvalidationCascades.None);

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

        return (review.ToResponse(), CacheInvalidationCascades.ForTag(CacheTags.Reviews));
    }
}
