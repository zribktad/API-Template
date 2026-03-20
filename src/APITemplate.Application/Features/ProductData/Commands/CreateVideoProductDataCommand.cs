using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.ProductData.Mappings;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.ProductData;

public sealed record CreateVideoProductDataCommand(CreateVideoProductDataRequest Request)
    : ICommand<ProductDataResponse>;

public sealed class CreateVideoProductDataCommandHandler
    : ICommandHandler<CreateVideoProductDataCommand, ProductDataResponse>
{
    private readonly IProductDataRepository _repository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEventPublisher _publisher;
    private readonly TimeProvider _timeProvider;

    public CreateVideoProductDataCommandHandler(
        IProductDataRepository repository,
        ITenantProvider tenantProvider,
        IEventPublisher publisher,
        TimeProvider timeProvider
    )
    {
        _repository = repository;
        _tenantProvider = tenantProvider;
        _publisher = publisher;
        _timeProvider = timeProvider;
    }

    public async Task<ProductDataResponse> HandleAsync(
        CreateVideoProductDataCommand command,
        CancellationToken ct
    )
    {
        var entity = new VideoProductData
        {
            TenantId = _tenantProvider.TenantId,
            Title = command.Request.Title,
            Description = command.Request.Description,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            DurationSeconds = command.Request.DurationSeconds,
            Resolution = command.Request.Resolution,
            Format = command.Request.Format,
            FileSizeBytes = command.Request.FileSizeBytes,
        };

        var created = await _repository.CreateAsync(entity, ct);
        await _publisher.PublishAsync(new ProductDataChangedNotification(), ct);
        return created.ToResponse();
    }
}
