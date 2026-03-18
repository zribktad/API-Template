using System.Runtime.CompilerServices;
using APITemplate.Application.Features.Examples.DTOs;
using MediatR;

namespace APITemplate.Application.Features.Examples.Handlers;

public sealed record GetNotificationStreamQuery(int Count = 5)
    : IRequest<IAsyncEnumerable<SseNotificationItem>>;

public sealed class SseStreamHandler
    : IRequestHandler<GetNotificationStreamQuery, IAsyncEnumerable<SseNotificationItem>>
{
    private readonly TimeProvider _timeProvider;

    public SseStreamHandler(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<IAsyncEnumerable<SseNotificationItem>> Handle(
        GetNotificationStreamQuery request,
        CancellationToken ct
    )
    {
        return Task.FromResult(StreamNotifications(request.Count, ct));
    }

    private async IAsyncEnumerable<SseNotificationItem> StreamNotifications(
        int count,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        for (var i = 1; i <= count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(500, ct);
            yield return new SseNotificationItem(
                i,
                $"Event {i} of {count}",
                _timeProvider.GetUtcNow().UtcDateTime
            );
        }
    }
}
