using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Mappings;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product;

/// <summary>Creates a new product from the supplied request data.</summary>
public sealed record CreateProductCommand(CreateProductRequest Request) : ICommand<ProductResponse>;

/// <summary>Handles <see cref="CreateProductCommand"/> by validating references, creating the product in a transaction, and publishing a change notification.</summary>
public sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, ProductResponse>
{
    private readonly IProductRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductDataRepository _productDataRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public CreateProductCommandHandler(
        IProductRepository repository,
        ICategoryRepository categoryRepository,
        IProductDataRepository productDataRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _productDataRepository = productDataRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<ProductResponse> HandleAsync(
        CreateProductCommand command,
        CancellationToken ct
    )
    {
        await ProductValidationHelper.ValidateCategoryExistsAsync(
            _categoryRepository,
            command.Request.CategoryId,
            ct
        );
        var productDataIds = await ProductValidationHelper.ValidateAndNormalizeProductDataIdsAsync(
            _productDataRepository,
            command.Request.ProductDataIds ?? [],
            ct
        );

        var product = await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                var productId = Guid.NewGuid();
                var entity = new ProductEntity
                {
                    Id = productId,
                    Name = command.Request.Name,
                    Description = command.Request.Description,
                    Price = command.Request.Price,
                    CategoryId = command.Request.CategoryId,
                    ProductDataLinks = productDataIds
                        .Select(productDataId => ProductDataLink.Create(productId, productDataId))
                        .ToList(),
                };

                await _repository.AddAsync(entity, ct);
                return entity;
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Products), ct);
        return product.ToResponse();
    }
}
