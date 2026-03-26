using ErrorOr;
using ProductCatalog.Application.Features.ProductData.DTOs;
using ProductCatalog.Application.Features.ProductData.Mappings;
using ProductCatalog.Domain.Entities.ProductData;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Application.Context;
using ProductDataEntity = ProductCatalog.Domain.Entities.ProductData.ProductData;

namespace ProductCatalog.Application.Features.ProductData.Commands;

public sealed record CreateVideoProductDataCommand(CreateVideoProductDataRequest Request);

public sealed class CreateVideoProductDataCommandHandler
{
    public static async Task<ErrorOr<ProductDataResponse>> HandleAsync(
        CreateVideoProductDataCommand command,
        IProductDataRepository repository,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        VideoProductData entity = new()
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

        ProductDataEntity created = await repository.CreateAsync(entity, ct);
        return created.ToResponse();
    }
}
