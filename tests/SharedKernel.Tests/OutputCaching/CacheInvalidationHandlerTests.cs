using Moq;
using SharedKernel.Api.OutputCaching;
using SharedKernel.Application.Common.Events;
using Xunit;

namespace SharedKernel.Tests.OutputCaching;

public sealed class CacheInvalidationHandlerTests
{
    [Fact]
    public async Task HandleAsync_EvictsProvidedTag()
    {
        var serviceMock = new Mock<IOutputCacheInvalidationService>();
        var message = new CacheInvalidationNotification("Products");

        await CacheInvalidationHandler.HandleAsync(
            message,
            serviceMock.Object,
            TestContext.Current.CancellationToken
        );

        serviceMock.Verify(
            x => x.EvictAsync("Products", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
