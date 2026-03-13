using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Resilience;
using APITemplate.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly.Registry;

namespace APITemplate.Application.Features.ProductData.Handlers;

public sealed class ProductDataCascadeDeleteHandler
    : INotificationHandler<TenantSoftDeletedNotification>
{
    private readonly IProductDataRepository _productDataRepository;
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;
    private readonly ILogger<ProductDataCascadeDeleteHandler> _logger;

    public ProductDataCascadeDeleteHandler(
        IProductDataRepository productDataRepository,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        ILogger<ProductDataCascadeDeleteHandler> logger
    )
    {
        _productDataRepository = productDataRepository;
        _resiliencePipelineProvider = resiliencePipelineProvider;
        _logger = logger;
    }

    public async Task Handle(TenantSoftDeletedNotification notification, CancellationToken ct)
    {
        var pipeline = _resiliencePipelineProvider.GetPipeline(
            ResiliencePipelineKeys.MongoProductDataDelete
        );

        try
        {
            var count = await pipeline.ExecuteAsync(
                async token =>
                    await _productDataRepository.SoftDeleteByTenantAsync(
                        notification.TenantId,
                        notification.ActorId,
                        notification.DeletedAtUtc,
                        token
                    ),
                ct
            );

            _logger.LogInformation(
                "Cascade soft-deleted {Count} ProductData documents for tenant {TenantId}.",
                count,
                notification.TenantId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cascade soft-delete ProductData documents for tenant {TenantId}. EF entities are already soft-deleted.",
                notification.TenantId
            );
        }
    }
}
