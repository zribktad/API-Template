using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Category;

/// <summary>Updates an existing category's name and description.</summary>
public sealed record UpdateCategoryCommand(Guid Id, UpdateCategoryRequest Request) : ICommand;

/// <summary>Handles <see cref="UpdateCategoryCommand"/>.</summary>
public sealed class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand>
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public UpdateCategoryCommandHandler(
        ICategoryRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task HandleAsync(UpdateCategoryCommand command, CancellationToken ct)
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

        await _publisher.PublishAsync(new CategoriesChangedNotification(), ct);
    }
}
