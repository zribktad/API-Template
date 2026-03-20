using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Category.Mappings;
using APITemplate.Domain.Interfaces;
using CategoryEntity = APITemplate.Domain.Entities.Category;

namespace APITemplate.Application.Features.Category;

/// <summary>Creates a new category and returns the persisted representation.</summary>
public sealed record CreateCategoryCommand(CreateCategoryRequest Request)
    : ICommand<CategoryResponse>;

/// <summary>Handles <see cref="CreateCategoryCommand"/>.</summary>
public sealed class CreateCategoryCommandHandler
    : ICommandHandler<CreateCategoryCommand, CategoryResponse>
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public CreateCategoryCommandHandler(
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<CategoryResponse> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken ct
    )
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

        await _publisher.PublishAsync(new CategoriesChangedNotification(), ct);
        return category.ToResponse();
    }
}
