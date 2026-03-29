using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Application.Common.Events;
using Shouldly;
using Wolverine;
using Xunit;

namespace APITemplate.Tests.Integration.Infrastructure;

public sealed class WolverineNotificationTests : IClassFixture<AlbaApiFixture>
{
    private readonly AlbaApiFixture _fixture;

    public WolverineNotificationTests(AlbaApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CacheInvalidationNotification_CanBePublished()
    {
        await using var scope = _fixture.Host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var exception = await Record.ExceptionAsync(() =>
            bus.PublishAsync(new CacheInvalidationNotification(CacheTags.Products)).AsTask()
        );

        exception.ShouldBeNull();
    }
}
