using APITemplate.Application.Common.Mediator;
using APITemplate.Application.Features.ProductData.Mappings;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace APITemplate.Application.Features.ProductData.Mediator;

public sealed record GetProductDataQuery(string? Type) : IQuery<List<ProductDataResponse>>;

public sealed record GetProductDataByIdQuery(string Id) : IQuery<ProductDataResponse?>;

public sealed record CreateImageProductDataCommand(CreateImageProductDataRequest Request) : ICommand<ProductDataResponse>;

public sealed record CreateVideoProductDataCommand(CreateVideoProductDataRequest Request) : ICommand<ProductDataResponse>;

public sealed record DeleteProductDataCommand(string Id) : ICommand;

public sealed class GetProductDataQueryHandler : IRequestHandler<GetProductDataQuery, List<ProductDataResponse>>
{
    private readonly IProductDataRepository _repository;

    public GetProductDataQueryHandler(IProductDataRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ProductDataResponse>> Handle(GetProductDataQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync(request.Type, cancellationToken);
        return items.Select(x => x.ToResponse()).ToList();
    }
}

public sealed class GetProductDataByIdQueryHandler : IRequestHandler<GetProductDataByIdQuery, ProductDataResponse?>
{
    private readonly IProductDataRepository _repository;

    public GetProductDataByIdQueryHandler(IProductDataRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductDataResponse?> Handle(GetProductDataByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return item?.ToResponse();
    }
}

public sealed class CreateImageProductDataCommandHandler : IRequestHandler<CreateImageProductDataCommand, ProductDataResponse>
{
    private readonly IProductDataRepository _repository;

    public CreateImageProductDataCommandHandler(IProductDataRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductDataResponse> Handle(CreateImageProductDataCommand request, CancellationToken cancellationToken)
    {
        var entity = new ImageProductData
        {
            Title = request.Request.Title,
            Description = request.Request.Description,
            Width = request.Request.Width,
            Height = request.Request.Height,
            Format = request.Request.Format,
            FileSizeBytes = request.Request.FileSizeBytes
        };

        var created = await _repository.CreateAsync(entity, cancellationToken);
        return created.ToResponse();
    }
}

public sealed class CreateVideoProductDataCommandHandler : IRequestHandler<CreateVideoProductDataCommand, ProductDataResponse>
{
    private readonly IProductDataRepository _repository;

    public CreateVideoProductDataCommandHandler(IProductDataRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductDataResponse> Handle(CreateVideoProductDataCommand request, CancellationToken cancellationToken)
    {
        var entity = new VideoProductData
        {
            Title = request.Request.Title,
            Description = request.Request.Description,
            DurationSeconds = request.Request.DurationSeconds,
            Resolution = request.Request.Resolution,
            Format = request.Request.Format,
            FileSizeBytes = request.Request.FileSizeBytes
        };

        var created = await _repository.CreateAsync(entity, cancellationToken);
        return created.ToResponse();
    }
}

public sealed class DeleteProductDataCommandHandler : IRequestHandler<DeleteProductDataCommand>
{
    private readonly IProductDataRepository _repository;

    public DeleteProductDataCommandHandler(IProductDataRepository repository)
    {
        _repository = repository;
    }

    public Task Handle(DeleteProductDataCommand request, CancellationToken cancellationToken)
        => _repository.DeleteAsync(request.Id, cancellationToken);
}

public sealed class GetProductDataByIdQueryValidator : AbstractValidator<GetProductDataByIdQuery>
{
    public GetProductDataByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteProductDataCommandValidator : AbstractValidator<DeleteProductDataCommand>
{
    public DeleteProductDataCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class CreateImageProductDataCommandValidator : AbstractValidator<CreateImageProductDataCommand>
{
    public CreateImageProductDataCommandValidator(IValidator<CreateImageProductDataRequest> requestValidator)
    {
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}

public sealed class CreateVideoProductDataCommandValidator : AbstractValidator<CreateVideoProductDataCommand>
{
    public CreateVideoProductDataCommandValidator(IValidator<CreateVideoProductDataRequest> requestValidator)
    {
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}
