using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.ProductReview.Mappings;
using APITemplate.Application.Features.ProductReview.Specifications;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;
using ProductReviewEntity = APITemplate.Domain.Entities.ProductReview;

namespace APITemplate.Application.Features.ProductReview;

/// <summary>Returns a paginated, filtered, and sorted list of product reviews.</summary>
public sealed record GetProductReviewsQuery(ProductReviewFilter Filter)
    : IRequest<PagedResponse<ProductReviewResponse>>;

/// <summary>Returns a single product review by its unique identifier, or <see langword="null"/> if not found.</summary>
public sealed record GetProductReviewByIdQuery(Guid Id) : IRequest<ProductReviewResponse?>;

/// <summary>Returns all reviews for a specific product, ordered by creation date descending.</summary>
public sealed record GetProductReviewsByProductIdQuery(Guid ProductId)
    : IRequest<IReadOnlyList<ProductReviewResponse>>;

/// <summary>
/// Returns reviews grouped by product id for a batch of product identifiers.
/// Products with no reviews are included in the result with an empty array.
/// </summary>
public sealed record GetProductReviewsByProductIdsQuery(IReadOnlyCollection<Guid> ProductIds)
    : IRequest<IReadOnlyDictionary<Guid, ProductReviewResponse[]>>;

/// <summary>Creates a new product review for the authenticated user and returns the persisted representation.</summary>
public sealed record CreateProductReviewCommand(CreateProductReviewRequest Request)
    : IRequest<ProductReviewResponse>;

/// <summary>Deletes the product review with the given identifier; only the review's author may delete it.</summary>
public sealed record DeleteProductReviewCommand(Guid Id) : IRequest;

/// <summary>
/// MediatR handler that processes all product review queries and commands in the Application layer.
/// Publishes a <see cref="ProductReviewsChangedNotification"/> after every write operation.
/// </summary>
public sealed class ProductReviewRequestHandlers
    : IRequestHandler<GetProductReviewsQuery, PagedResponse<ProductReviewResponse>>,
        IRequestHandler<GetProductReviewByIdQuery, ProductReviewResponse?>,
        IRequestHandler<GetProductReviewsByProductIdQuery, IReadOnlyList<ProductReviewResponse>>,
        IRequestHandler<
            GetProductReviewsByProductIdsQuery,
            IReadOnlyDictionary<Guid, ProductReviewResponse[]>
        >,
        IRequestHandler<CreateProductReviewCommand, ProductReviewResponse>,
        IRequestHandler<DeleteProductReviewCommand>
{
    private readonly IProductReviewRepository _reviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActorProvider _actorProvider;
    private readonly IPublisher _publisher;

    public ProductReviewRequestHandlers(
        IProductReviewRepository reviewRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IActorProvider actorProvider,
        IPublisher publisher
    )
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _actorProvider = actorProvider;
        _publisher = publisher;
    }

    /// <summary>Fetches a filtered, sorted, and paginated page of product reviews.</summary>
    public async Task<PagedResponse<ProductReviewResponse>> Handle(
        GetProductReviewsQuery request,
        CancellationToken ct
    )
    {
        return await _reviewRepository.GetPagedAsync(
            new ProductReviewSpecification(request.Filter),
            new ProductReviewCountSpecification(request.Filter),
            request.Filter.PageNumber,
            request.Filter.PageSize,
            ct
        );
    }

    /// <summary>Fetches a single review by id and maps the domain entity to a response DTO.</summary>
    public async Task<ProductReviewResponse?> Handle(
        GetProductReviewByIdQuery request,
        CancellationToken ct
    )
    {
        var item = await _reviewRepository.GetByIdAsync(request.Id, ct);
        return item?.ToResponse();
    }

    /// <summary>Fetches all reviews for a single product, ordered newest-first, using a dedicated specification.</summary>
    public async Task<IReadOnlyList<ProductReviewResponse>> Handle(
        GetProductReviewsByProductIdQuery request,
        CancellationToken ct
    ) =>
        await _reviewRepository.ListAsync(
            new ProductReviewByProductIdSpecification(request.ProductId),
            ct
        );

    /// <summary>
    /// Fetches reviews for a batch of product ids in a single query and groups them into a lookup dictionary.
    /// Returns an empty dictionary when <see cref="GetProductReviewsByProductIdsQuery.ProductIds"/> is empty.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, ProductReviewResponse[]>> Handle(
        GetProductReviewsByProductIdsQuery request,
        CancellationToken ct
    )
    {
        if (request.ProductIds.Count == 0)
            return new Dictionary<Guid, ProductReviewResponse[]>();

        var reviews = await _reviewRepository.ListAsync(
            new ProductReviewByProductIdsSpecification(request.ProductIds),
            ct
        );
        var lookup = reviews.ToLookup(review => review.ProductId);

        return request.ProductIds.Distinct().ToDictionary(id => id, id => lookup[id].ToArray());
    }

    /// <summary>
    /// Creates a new review inside a transaction after verifying the target product exists.
    /// Throws <see cref="NotFoundException"/> when the referenced product does not exist.
    /// </summary>
    public async Task<ProductReviewResponse> Handle(
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

        await _publisher.Publish(new ProductReviewsChangedNotification(), ct);
        return review.ToResponse();
    }

    /// <summary>
    /// Deletes a review after confirming ownership by the authenticated actor.
    /// Throws <see cref="NotFoundException"/> if the review does not exist and <see cref="ForbiddenException"/> if the actor is not the author.
    /// </summary>
    public async Task Handle(DeleteProductReviewCommand command, CancellationToken ct)
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

        await _publisher.Publish(new ProductReviewsChangedNotification(), ct);
    }
}
