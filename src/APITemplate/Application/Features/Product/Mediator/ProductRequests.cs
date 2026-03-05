using APITemplate.Application.Common.Mediator;
using APITemplate.Application.Features.Product.Mappings;
using APITemplate.Application.Features.Product.Specifications;
using ProductEntity = APITemplate.Domain.Entities.Product;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace APITemplate.Application.Features.Product.Mediator;

public sealed record GetProductsQuery(ProductFilter Filter) : IQuery<PagedResponse<ProductResponse>>;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse?>;

public sealed record CreateProductCommand(CreateProductRequest Request) : ICommand<ProductResponse>;

public sealed record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : ICommand;

public sealed record DeleteProductCommand(Guid Id) : ICommand;

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResponse<ProductResponse>>
{
    private readonly IProductRepository _repository;

    public GetProductsQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<ProductResponse>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.ListAsync(new ProductSpecification(request.Filter), cancellationToken);
        var totalCount = await _repository.CountAsync(new ProductCountSpecification(request.Filter), cancellationToken);
        return new PagedResponse<ProductResponse>(items, totalCount, request.Filter.PageNumber, request.Filter.PageSize);
    }
}

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductResponse?>
{
    private readonly IProductRepository _repository;

    public GetProductByIdQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity?.ToResponse();
    }
}

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Request.Name,
            Description = request.Request.Description,
            Price = request.Request.Price,
            CategoryId = request.Request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(product, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return product.ToResponse();
    }
}

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductEntity), request.Id, ErrorCatalog.Products.NotFound);

        product.Name = request.Request.Name;
        product.Description = request.Request.Description;
        product.Price = request.Request.Price;
        product.CategoryId = request.Request.CategoryId;

        await _repository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator(IValidator<ProductFilter> filterValidator)
    {
        RuleFor(x => x.Filter).SetValidator(filterValidator);
    }
}

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IValidator<CreateProductRequest> requestValidator)
    {
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IValidator<UpdateProductRequest> requestValidator)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}

public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
