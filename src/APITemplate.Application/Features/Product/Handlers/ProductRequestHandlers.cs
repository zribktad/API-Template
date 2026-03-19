using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Mappings;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.Product.Specifications;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product;

/// <summary>Retrieves a single product by its unique identifier.</summary>
public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductResponse?>;

/// <summary>Retrieves a filtered, sorted, and paged list of products together with search facets.</summary>
public sealed record GetProductsQuery(ProductFilter Filter) : IRequest<ProductsResponse>;

/// <summary>Creates a new product from the supplied request data.</summary>
public sealed record CreateProductCommand(CreateProductRequest Request) : IRequest<ProductResponse>;

/// <summary>Replaces the details of an existing product identified by <paramref name="Id"/>.</summary>
public sealed record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : IRequest;

/// <summary>Soft-deletes a product and its associated data links.</summary>
public sealed record DeleteProductCommand(Guid Id) : IRequest;

/// <summary>
/// Application-layer handler that processes all product CRUD commands and queries using MediatR.
/// Coordinates the product repository, category/product-data validation, unit-of-work transactions, and domain-event publishing.
/// </summary>
public sealed class ProductRequestHandlers
    : IRequestHandler<GetProductByIdQuery, ProductResponse?>,
        IRequestHandler<GetProductsQuery, ProductsResponse>,
        IRequestHandler<CreateProductCommand, ProductResponse>,
        IRequestHandler<UpdateProductCommand>,
        IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductDataRepository _productDataRepository;
    private readonly IProductDataLinkRepository _productDataLinkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public ProductRequestHandlers(
        IProductRepository repository,
        ICategoryRepository categoryRepository,
        IProductDataRepository productDataRepository,
        IProductDataLinkRepository productDataLinkRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher
    )
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _productDataRepository = productDataRepository;
        _productDataLinkRepository = productDataLinkRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    /// <summary>Returns the product matching the requested ID, or <see langword="null"/> if not found.</summary>
    public async Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken ct) =>
        await _repository.FirstOrDefaultAsync(new ProductByIdSpecification(request.Id), ct);

    /// <summary>Fetches a filtered product page and computes category and price facets in parallel repository calls.</summary>
    public async Task<ProductsResponse> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var items = await _repository.ListAsync(request.Filter, ct);
        var totalCount = await _repository.CountAsync(request.Filter, ct);
        var categoryFacets = await _repository.GetCategoryFacetsAsync(request.Filter, ct);
        var priceFacets = await _repository.GetPriceFacetsAsync(request.Filter, ct);

        return new ProductsResponse(
            new PagedResponse<ProductResponse>(
                items,
                totalCount,
                request.Filter.PageNumber,
                request.Filter.PageSize
            ),
            new ProductSearchFacetsResponse(categoryFacets, priceFacets)
        );
    }

    /// <summary>Validates the referenced category and product-data IDs, then creates the product and its data links in a single transaction before publishing a change notification.</summary>
    public async Task<ProductResponse> Handle(CreateProductCommand command, CancellationToken ct)
    {
        await ValidateCategoryExistsAsync(command.Request.CategoryId, ct);
        var productDataIds = await ValidateAndNormalizeProductDataIdsAsync(
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

        await _publisher.Publish(new ProductsChangedNotification(), ct);
        return product.ToResponse();
    }

    /// <summary>Loads the product with its links, validates the new category, updates domain details, synchronises product-data links within a transaction, and publishes a change notification.</summary>
    public async Task Handle(UpdateProductCommand command, CancellationToken ct)
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

        await ValidateCategoryExistsAsync(command.Request.CategoryId, ct);

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
                    var productDataIds = await ValidateAndNormalizeProductDataIdsAsync(
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

        await _publisher.Publish(new ProductsChangedNotification(), ct);
    }

    /// <summary>Soft-deletes the product's data links and then soft-deletes the product itself in a transaction, publishing both product and review change notifications.</summary>
    public async Task Handle(DeleteProductCommand command, CancellationToken ct)
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

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                product.SoftDeleteProductDataLinks();
                await _repository.DeleteAsync(product, ct);
            },
            ct
        );

        await _publisher.Publish(new ProductsChangedNotification(), ct);
        await _publisher.Publish(new ProductReviewsChangedNotification(), ct);
    }

    /// <summary>Throws <see cref="NotFoundException"/> if a non-null <paramref name="categoryId"/> does not correspond to an existing category.</summary>
    private async Task ValidateCategoryExistsAsync(Guid? categoryId, CancellationToken ct)
    {
        if (!categoryId.HasValue)
            return;

        _ =
            await _categoryRepository.GetByIdAsync(categoryId.Value, ct)
            ?? throw new NotFoundException(
                nameof(Category),
                categoryId.Value,
                ErrorCatalog.Categories.NotFound
            );
    }

    /// <summary>Deduplicates the supplied IDs, verifies each exists in the repository, and throws <see cref="NotFoundException"/> for any missing entries.</summary>
    private async Task<IReadOnlyCollection<Guid>> ValidateAndNormalizeProductDataIdsAsync(
        IReadOnlyCollection<Guid> productDataIds,
        CancellationToken ct
    )
    {
        var normalizedIds = productDataIds.Distinct().ToArray();

        if (normalizedIds.Length == 0)
            return normalizedIds;

        var existingIds = (await _productDataRepository.GetByIdsAsync(normalizedIds, ct))
            .Select(productData => productData.Id)
            .ToHashSet();

        var missingIds = normalizedIds.Where(id => !existingIds.Contains(id)).ToArray();

        if (missingIds.Length > 0)
        {
            throw new NotFoundException(
                nameof(ProductData),
                string.Join(", ", missingIds),
                ErrorCatalog.Products.ProductDataNotFound
            );
        }

        return normalizedIds;
    }
}
