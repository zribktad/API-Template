using Contracts.IntegrationEvents.ProductCatalog;
using Contracts.IntegrationEvents.Sagas;
using FileStorage.Application.Features.Files.EventHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Wolverine;
using Xunit;

namespace FileStorage.Tests.Features.Files.EventHandlers;

public sealed class ProductDeletedEventHandlerTests
{
    private readonly Mock<IMessageBus> _busMock = new();
    private readonly Mock<ILogger<ProductDeletedEventHandler>> _loggerMock = new();

    [Fact]
    public async Task HandleAsync_PublishesFilesCascadeCompleted()
    {
        Guid correlationId = Guid.NewGuid();
        IReadOnlyList<Guid> productIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        ProductDeletedIntegrationEvent @event = new(
            correlationId,
            productIds,
            Guid.NewGuid(),
            DateTime.UtcNow
        );
        FilesCascadeCompleted? capturedMessage = null;
        _busMock
            .Setup(b => b.PublishAsync(It.IsAny<FilesCascadeCompleted>(), null))
            .Callback<object, DeliveryOptions?>(
                (msg, _) => capturedMessage = msg as FilesCascadeCompleted
            )
            .Returns(ValueTask.CompletedTask);

        await ProductDeletedEventHandler.HandleAsync(
            @event,
            _busMock.Object,
            _loggerMock.Object,
            CancellationToken.None
        );

        capturedMessage.ShouldNotBeNull();
        capturedMessage.ProductDeletionSagaId.ShouldBe(correlationId.ToString());
        capturedMessage.DeletedCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_PublishesExactlyOneMessage()
    {
        ProductDeletedIntegrationEvent @event = new(
            Guid.NewGuid(),
            new[] { Guid.NewGuid() },
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        await ProductDeletedEventHandler.HandleAsync(
            @event,
            _busMock.Object,
            _loggerMock.Object,
            CancellationToken.None
        );

        _busMock.Verify(b => b.PublishAsync(It.IsAny<FilesCascadeCompleted>(), null), Times.Once);
    }
}
