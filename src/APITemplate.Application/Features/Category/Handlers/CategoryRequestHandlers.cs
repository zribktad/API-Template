using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.Category.Mappings;
using APITemplate.Application.Features.Category.Specifications;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;
using CategoryEntity = APITemplate.Domain.Entities.Category;

namespace APITemplate.Application.Features.Category;

/// <summary>Returns a paginated, filtered, and sorted list of categories.</summary>
public sealed record GetCategoriesQuery(CategoryFilter Filter)
    : IRequest<PagedResponse<CategoryResponse>>;

/// <summary>Returns a single category by its unique identifier, or <see langword="null"/> if not found.</summary>
public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryResponse?>;

/// <summary>Returns aggregated statistics for a category by its identifier, or <see langword="null"/> if not found.</summary>
public sealed record GetCategoryStatsQuery(Guid Id) : IRequest<ProductCategoryStatsResponse?>;

/// <summary>Creates a new category and returns the persisted representation.</summary>
public sealed record CreateCategoryCommand(CreateCategoryRequest Request)
    : IRequest<CategoryResponse>;

/// <summary>Updates an existing category's name and description.</summary>
public sealed record UpdateCategoryCommand(Guid Id, UpdateCategoryRequest Request) : IRequest;

/// <summary>Deletes a category by its unique identifier.</summary>
public sealed record DeleteCategoryCommand(Guid Id) : IRequest;

/// <summary>
/// MediatR handler that processes all category CRUD queries and commands in the Application layer.
/// Publishes a <see cref="CategoriesChangedNotification"/> after every write operation.
/// </summary>
public sealed class CategoryRequestHandlers
    : IRequestHandler<GetCategoriesQuery, PagedResponse<CategoryResponse>>,
        IRequestHandler<GetCategoryByIdQuery, CategoryResponse?>,
        IRequestHandler<GetCategoryStatsQuery, ProductCategoryStatsResponse?>,
        IRequestHandler<CreateCategoryCommand, CategoryResponse>,
        IRequestHandler<UpdateCategoryCommand>,
        IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public CategoryRequestHandlers(
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        IPublisher publisher
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    /// <summary>Fetches a filtered, sorted, and paginated list of categories.</summary>
    public async Task<PagedResponse<CategoryResponse>> Handle(
        GetCategoriesQuery request,
        CancellationToken ct
    )
    {
        return await _repository.GetPagedAsync(
            new CategorySpecification(request.Filter),
            new CategoryCountSpecification(request.Filter),
            request.Filter.PageNumber,
            request.Filter.PageSize,
            ct
        );
    }

    /// <summary>Fetches a single category projected directly to <see cref="CategoryResponse"/>.</summary>
    public async Task<CategoryResponse?> Handle(
        GetCategoryByIdQuery request,
        CancellationToken ct
    ) => await _repository.FirstOrDefaultAsync(new CategoryByIdSpecification(request.Id), ct);

    /// <summary>Fetches aggregated product statistics for the requested category and maps them to a response DTO.</summary>
    public async Task<ProductCategoryStatsResponse?> Handle(
        GetCategoryStatsQuery request,
        CancellationToken ct
    )
    {
        var stats = await _repository.GetStatsByIdAsync(request.Id, ct);
        return stats?.ToResponse();
    }

    /// <summary>Creates a new category entity inside a transaction and publishes a change notification.</summary>
    public async Task<CategoryResponse> Handle(CreateCategoryCommand command, CancellationToken ct)
    {
        var category = await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                var entity = new CategoryEntity
                {
                    Id = Guid.NewGuid(),
                    Name = command.Request.Name,
                    Description = command.Request.Description,
                };

                await _repository.AddAsync(entity, ct);
                return entity;
            },
            ct
        );

        await _publisher.Publish(new CategoriesChangedNotification(), ct);
        return category.ToResponse();
    }

    /// <summary>
    /// Updates the name and description of an existing category inside a transaction.
    /// Throws <see cref="NotFoundException"/> when the category does not exist.
    /// </summary>
    public async Task Handle(UpdateCategoryCommand command, CancellationToken ct)
    {
        var category = await _repository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Categories.NotFound,
            ct
        );

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                category.Name = command.Request.Name;
                category.Description = command.Request.Description;

                await _repository.UpdateAsync(category, ct);
            },
            ct
        );

        await _publisher.Publish(new CategoriesChangedNotification(), ct);
    }

    /// <summary>
    /// Deletes a category by id inside a transaction.
    /// Throws <see cref="NotFoundException"/> when the category does not exist.
    /// </summary>
    public async Task Handle(DeleteCategoryCommand command, CancellationToken ct)
    {
        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.DeleteAsync(command.Id, ct, ErrorCatalog.Categories.NotFound);
            },
            ct
        );

        await _publisher.Publish(new CategoriesChangedNotification(), ct);
    }
}
