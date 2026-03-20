using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Resilience;
using APITemplate.Application.Features.ProductData.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Handlers;

public sealed class ProductDataCascadeDeleteHandlerTests
{
    private readonly Mock<IProductDataRepository> _productDataRepositoryMock;
    private readonly Mock<ILogger<ProductDataCascadeDeleteHandler>> _loggerMock;
    private readonly ProductDataCascadeDeleteHandler _sut;

    public ProductDataCascadeDeleteHandlerTests()
    {
        _productDataRepositoryMock = new Mock<IProductDataRepository>();
        _loggerMock = new Mock<ILogger<ProductDataCascadeDeleteHandler>>();

        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(ResiliencePipelineKeys.MongoProductDataDelete, (builder, _) => { });

        _sut = new ProductDataCascadeDeleteHandler(
            _productDataRepositoryMock.Object,
            registry,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_WhenTenantSoftDeleted_CallsSoftDeleteByTenantAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var deletedAt = DateTime.UtcNow;
        var notification = new TenantSoftDeletedNotification(tenantId, actorId, deletedAt);

        _productDataRepositoryMock
            .Setup(r =>
                r.SoftDeleteByTenantAsync(
                    tenantId,
                    actorId,
                    deletedAt,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(5);

        await _sut.HandleAsync(notification, ct);

        _productDataRepositoryMock.Verify(
            r =>
                r.SoftDeleteByTenantAsync(
                    tenantId,
                    actorId,
                    deletedAt,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenTenantSoftDeleted_LogsDeletedCount()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        var notification = new TenantSoftDeletedNotification(
            tenantId,
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        _productDataRepositoryMock
            .Setup(r =>
                r.SoftDeleteByTenantAsync(
                    tenantId,
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(3);

        await _sut.HandleAsync(notification, ct);

        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, _) =>
                            state.ToString()!.Contains("3")
                            && state.ToString()!.Contains(tenantId.ToString())
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_LogsErrorAndDoesNotRethrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        var notification = new TenantSoftDeletedNotification(
            tenantId,
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        _productDataRepositoryMock
            .Setup(r =>
                r.SoftDeleteByTenantAsync(
                    tenantId,
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("MongoDB connection failed"));

        await Should.NotThrowAsync(() => _sut.HandleAsync(notification, ct));

        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, _) => state.ToString()!.Contains(tenantId.ToString())
                    ),
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenNoDocumentsToDelete_StillLogsInformation()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        var notification = new TenantSoftDeletedNotification(
            tenantId,
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        _productDataRepositoryMock
            .Setup(r =>
                r.SoftDeleteByTenantAsync(
                    tenantId,
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(0);

        await _sut.HandleAsync(notification, ct);

        _productDataRepositoryMock.Verify(
            r =>
                r.SoftDeleteByTenantAsync(
                    tenantId,
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _loggerMock.Verify(
            logger =>
                logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("0")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
