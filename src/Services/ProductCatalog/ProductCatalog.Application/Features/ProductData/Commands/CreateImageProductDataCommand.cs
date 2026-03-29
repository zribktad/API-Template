using ErrorOr;
using ProductCatalog.Application.Features.ProductData.DTOs;
using ProductCatalog.Application.Features.ProductData.Mappings;
using ProductCatalog.Domain.Entities.ProductData;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Context;
using Wolverine;
using ProductDataEntity = ProductCatalog.Domain.Entities.ProductData.ProductData;

namespace ProductCatalog.Application.Features.ProductData.Commands;

public sealed record CreateImageProductDataCommand(CreateImageProductDataRequest Request);

public sealed class CreateImageProductDataCommandHandler
{
    public static async Task<(ErrorOr<ProductDataResponse>, OutgoingMessages)> HandleAsync(
        CreateImageProductDataCommand command,
        IProductDataRepository repository,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        ImageProductData entity = new()
        {
            TenantId = tenantProvider.TenantId,
            Title = command.Request.Title,
            Description = command.Request.Description,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            Width = command.Request.Width,
            Height = command.Request.Height,
            Format = command.Request.Format,
            FileSizeBytes = command.Request.FileSizeBytes,
        };

        ProductDataEntity created = await repository.CreateAsync(entity, ct);
        return (created.ToResponse(), CacheInvalidationCascades.ForTag(CacheTags.ProductData));
    }
}
