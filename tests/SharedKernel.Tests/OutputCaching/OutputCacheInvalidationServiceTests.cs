using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Api.OutputCaching;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.OutputCaching;

public sealed class OutputCacheInvalidationServiceTests
{
    private readonly Mock<IOutputCacheStore> _storeMock = new();
    private readonly Mock<ILogger<OutputCacheInvalidationService>> _loggerMock = new();

    [Fact]
    public async Task EvictAsync_SingleTag_EvictsStoreOnce()
    {
        var sut = new OutputCacheInvalidationService(_storeMock.Object, _loggerMock.Object);

        await sut.EvictAsync("Products", TestContext.Current.CancellationToken);

        _storeMock.Verify(
            x => x.EvictByTagAsync("Products", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task EvictAsync_DuplicateTags_EvictsDistinctTagsOnly()
    {
        var sut = new OutputCacheInvalidationService(_storeMock.Object, _loggerMock.Object);

        await sut.EvictAsync(["Products", "Products", "Categories"]);

        _storeMock.Verify(
            x => x.EvictByTagAsync("Products", It.IsAny<CancellationToken>()),
            Times.Once
        );
        _storeMock.Verify(
            x => x.EvictByTagAsync("Categories", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task EvictAsync_WhenOneTagFails_ContinuesWithRemainingTags()
    {
        _storeMock
            .Setup(x => x.EvictByTagAsync("Products", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var sut = new OutputCacheInvalidationService(_storeMock.Object, _loggerMock.Object);

        var ex = await Record.ExceptionAsync(() =>
            sut.EvictAsync(["Products", "Categories"], TestContext.Current.CancellationToken)
        );

        ex.ShouldBeNull();
        _storeMock.Verify(
            x => x.EvictByTagAsync("Products", It.IsAny<CancellationToken>()),
            Times.Once
        );
        _storeMock.Verify(
            x => x.EvictByTagAsync("Categories", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
