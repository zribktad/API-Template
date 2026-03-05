using APITemplate.Application.Common.Mediator;
using APITemplate.Application.Features.Category.Mappings;
using CategoryEntity = APITemplate.Domain.Entities.Category;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace APITemplate.Application.Features.Category.Mediator;

public sealed record GetCategoriesQuery() : IQuery<IReadOnlyList<CategoryResponse>>;

public sealed record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryResponse?>;

public sealed record CreateCategoryCommand(CreateCategoryRequest Request) : ICommand<CategoryResponse>;

public sealed record UpdateCategoryCommand(Guid Id, UpdateCategoryRequest Request) : ICommand;

public sealed record DeleteCategoryCommand(Guid Id) : ICommand;

public sealed record GetCategoryStatsQuery(Guid Id) : IQuery<ProductCategoryStatsResponse?>;

public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    private readonly ICategoryRepository _repository;

    public GetCategoriesQueryHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CategoryResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _repository.ListAsync(cancellationToken);
        return categories.Select(c => c.ToResponse()).ToList();
    }
}

public sealed class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryResponse?>
{
    private readonly ICategoryRepository _repository;

    public GetCategoryByIdQueryHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<CategoryResponse?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return category?.ToResponse();
    }
}

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryResponse>
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(ICategoryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new CategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Request.Name,
            Description = request.Request.Description,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(category, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return category.ToResponse();
    }
}

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(ICategoryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CategoryEntity), request.Id, ErrorCatalog.Categories.NotFound);

        category.Name = request.Request.Name;
        category.Description = request.Request.Description;

        await _repository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(ICategoryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}

public sealed class GetCategoryStatsQueryHandler : IRequestHandler<GetCategoryStatsQuery, ProductCategoryStatsResponse?>
{
    private readonly ICategoryRepository _repository;

    public GetCategoryStatsQueryHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductCategoryStatsResponse?> Handle(GetCategoryStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _repository.GetStatsByIdAsync(request.Id, cancellationToken);
        return stats?.ToResponse();
    }
}

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty();
        RuleFor(x => x.Request.Description).MaximumLength(500);
    }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty();
        RuleFor(x => x.Request.Description).MaximumLength(500);
    }
}

public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetCategoryStatsQueryValidator : AbstractValidator<GetCategoryStatsQuery>
{
    public GetCategoryStatsQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
