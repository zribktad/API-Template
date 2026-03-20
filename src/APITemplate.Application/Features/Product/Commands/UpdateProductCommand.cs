using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.Product.Specifications;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product;

/// <summary>Replaces the details of an existing product identified by <paramref name="Id"/>.</summary>
public sealed record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : ICommand;

/// <summary>Handles <see cref="UpdateProductCommand"/> by loading the product, updating fields in a transaction, and publishing a change notification.</summary>
public sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductDataRepository _productDataRepository;
    private readonly IProductDataLinkRepository _productDataLinkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public UpdateProductCommandHandler(
        IProductRepository repository,
        ICategoryRepository categoryRepository,
        IProductDataRepository productDataRepository,
        IProductDataLinkRepository productDataLinkRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _productDataRepository = productDataRepository;
        _productDataLinkRepository = productDataLinkRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task HandleAsync(UpdateProductCommand command, CancellationToken ct)
    {
        var product =
            await _repository.FirstOrDefaultAsync(
                new ProductByIdWithLinksSpecification(command.Id),
                ct
            )
            ?? throw new NotFoundException(
                nameof(ProductEntity),
                command.Id,
                ErrorCatalog.Products.NotFound
            );

        await ProductValidationHelper.ValidateCategoryExistsAsync(
            _categoryRepository,
            command.Request.CategoryId,
            ct
        );

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                product.UpdateDetails(
                    command.Request.Name,
                    command.Request.Description,
                    command.Request.Price,
                    command.Request.CategoryId
                );

                if (command.Request.ProductDataIds is not null)
                {
                    var productDataIds =
                        await ProductValidationHelper.ValidateAndNormalizeProductDataIdsAsync(
                            _productDataRepository,
                            command.Request.ProductDataIds,
                            ct
                        );
                    var allLinks = await _productDataLinkRepository.ListByProductIdAsync(
                        command.Id,
                        includeDeleted: true,
                        ct
                    );
                    product.SyncProductDataLinks(productDataIds, allLinks);
                }

                await _repository.UpdateAsync(product, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Products), ct);
    }
}
