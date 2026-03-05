using APITemplate.Application.Common.Mediator;
using APITemplate.Application.Features.ProductReview.Mappings;
using APITemplate.Application.Features.ProductReview.Specifications;
using ProductReviewEntity = APITemplate.Domain.Entities.ProductReview;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace APITemplate.Application.Features.ProductReview.Mediator;

public sealed record GetProductReviewsQuery(ProductReviewFilter Filter) : IQuery<PagedResponse<ProductReviewResponse>>;

public sealed record GetProductReviewByIdQuery(Guid Id) : IQuery<ProductReviewResponse?>;

public sealed record GetProductReviewsByProductIdQuery(Guid ProductId) : IQuery<IReadOnlyList<ProductReviewResponse>>;

public sealed record GetProductReviewsByProductIdsQuery(IReadOnlyCollection<Guid> ProductIds) : IQuery<IReadOnlyDictionary<Guid, ProductReviewResponse[]>>;

public sealed record CreateProductReviewCommand(CreateProductReviewRequest Request) : ICommand<ProductReviewResponse>;

public sealed record DeleteProductReviewCommand(Guid Id) : ICommand;

public sealed class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, PagedResponse<ProductReviewResponse>>
{
    private readonly IProductReviewRepository _repository;

    public GetProductReviewsQueryHandler(IProductReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<ProductReviewResponse>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.ListAsync(new ProductReviewSpecification(request.Filter), cancellationToken);
        var totalCount = await _repository.CountAsync(new ProductReviewCountSpecification(request.Filter), cancellationToken);
        return new PagedResponse<ProductReviewResponse>(items, totalCount, request.Filter.PageNumber, request.Filter.PageSize);
    }
}

public sealed class GetProductReviewByIdQueryHandler : IRequestHandler<GetProductReviewByIdQuery, ProductReviewResponse?>
{
    private readonly IProductReviewRepository _repository;

    public GetProductReviewByIdQueryHandler(IProductReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductReviewResponse?> Handle(GetProductReviewByIdQuery request, CancellationToken cancellationToken)
    {
        var review = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return review?.ToResponse();
    }
}

public sealed class GetProductReviewsByProductIdQueryHandler : IRequestHandler<GetProductReviewsByProductIdQuery, IReadOnlyList<ProductReviewResponse>>
{
    private readonly IProductReviewRepository _repository;

    public GetProductReviewsByProductIdQueryHandler(IProductReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductReviewResponse>> Handle(GetProductReviewsByProductIdQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _repository.ListAsync(new ProductReviewByProductIdSpecification(request.ProductId), cancellationToken);
        return reviews;
    }
}

public sealed class GetProductReviewsByProductIdsQueryHandler : IRequestHandler<GetProductReviewsByProductIdsQuery, IReadOnlyDictionary<Guid, ProductReviewResponse[]>>
{
    private readonly IProductReviewRepository _repository;

    public GetProductReviewsByProductIdsQueryHandler(IProductReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyDictionary<Guid, ProductReviewResponse[]>> Handle(GetProductReviewsByProductIdsQuery request, CancellationToken cancellationToken)
    {
        if (request.ProductIds.Count == 0)
            return new Dictionary<Guid, ProductReviewResponse[]>();

        var reviews = await _repository.ListAsync(new ProductReviewByProductIdsSpecification(request.ProductIds), cancellationToken);
        var lookup = reviews.ToLookup(r => r.ProductId);

        return request.ProductIds
            .Distinct()
            .ToDictionary(id => id, id => lookup[id].ToArray());
    }
}

public sealed class CreateProductReviewCommandHandler : IRequestHandler<CreateProductReviewCommand, ProductReviewResponse>
{
    private readonly IProductReviewRepository _reviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductReviewCommandHandler(
        IProductReviewRepository reviewRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductReviewResponse> Handle(CreateProductReviewCommand request, CancellationToken cancellationToken)
    {
        var productExists = await _productRepository.GetByIdAsync(request.Request.ProductId, cancellationToken) is not null;
        if (!productExists)
            throw new NotFoundException("Product", request.Request.ProductId, ErrorCatalog.Reviews.ProductNotFoundForReview);

        var review = new ProductReviewEntity
        {
            Id = Guid.NewGuid(),
            ProductId = request.Request.ProductId,
            ReviewerName = request.Request.ReviewerName,
            Comment = request.Request.Comment,
            Rating = request.Request.Rating,
            CreatedAt = DateTime.UtcNow
        };

        await _reviewRepository.AddAsync(review, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return review.ToResponse();
    }
}

public sealed class DeleteProductReviewCommandHandler : IRequestHandler<DeleteProductReviewCommand>
{
    private readonly IProductReviewRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductReviewCommandHandler(IProductReviewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteProductReviewCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}

public sealed class GetProductReviewsQueryValidator : AbstractValidator<GetProductReviewsQuery>
{
    public GetProductReviewsQueryValidator(IValidator<ProductReviewFilter> filterValidator)
    {
        RuleFor(x => x.Filter).SetValidator(filterValidator);
    }
}

public sealed class CreateProductReviewCommandValidator : AbstractValidator<CreateProductReviewCommand>
{
    public CreateProductReviewCommandValidator(IValidator<CreateProductReviewRequest> requestValidator)
    {
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}

public sealed class GetProductReviewByIdQueryValidator : AbstractValidator<GetProductReviewByIdQuery>
{
    public GetProductReviewByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetProductReviewsByProductIdQueryValidator : AbstractValidator<GetProductReviewsByProductIdQuery>
{
    public GetProductReviewsByProductIdQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public sealed class GetProductReviewsByProductIdsQueryValidator : AbstractValidator<GetProductReviewsByProductIdsQuery>
{
    public GetProductReviewsByProductIdsQueryValidator()
    {
        RuleFor(x => x.ProductIds).NotNull();
        RuleForEach(x => x.ProductIds).NotEmpty();
    }
}

public sealed class DeleteProductReviewCommandValidator : AbstractValidator<DeleteProductReviewCommand>
{
    public DeleteProductReviewCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
