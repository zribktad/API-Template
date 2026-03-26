using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.ProductData.Mappings;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;

namespace APITemplate.Application.Features.ProductData;

/// <summary>Creates a video product-data document in MongoDB.</summary>
public sealed record CreateVideoProductDataCommand(CreateVideoProductDataRequest Request);

public sealed class CreateVideoProductDataCommandHandler
{
    public static async Task<(ErrorOr<ProductDataResponse>, OutgoingMessages)> HandleAsync(
        CreateVideoProductDataCommand command,
        IProductDataRepository repository,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        var entity = new VideoProductData
        {
            TenantId = tenantProvider.TenantId,
            Title = command.Request.Title,
            Description = command.Request.Description,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            DurationSeconds = command.Request.DurationSeconds,
            Resolution = command.Request.Resolution,
            Format = command.Request.Format,
            FileSizeBytes = command.Request.FileSizeBytes,
        };

        var created = await repository.CreateAsync(entity, ct);
        return (created.ToResponse(), [new CacheInvalidationNotification(CacheTags.ProductData)]);
    }
}
