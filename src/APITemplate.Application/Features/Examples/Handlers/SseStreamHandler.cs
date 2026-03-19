using System.Runtime.CompilerServices;
using APITemplate.Application.Features.Examples.DTOs;
using MediatR;

namespace APITemplate.Application.Features.Examples.Handlers;

/// <summary>Requests an async-enumerable stream of SSE notification items based on the parameters in <see cref="SseStreamRequest"/>.</summary>
public sealed record GetNotificationStreamQuery(SseStreamRequest Request)
    : IRequest<IAsyncEnumerable<SseNotificationItem>>;

/// <summary>
/// Application-layer handler that produces a time-delayed async-enumerable stream of <see cref="SseNotificationItem"/> events for Server-Sent Events delivery.
/// </summary>
public sealed class SseStreamHandler
    : IRequestHandler<GetNotificationStreamQuery, IAsyncEnumerable<SseNotificationItem>>
{
    private readonly TimeProvider _timeProvider;

    public SseStreamHandler(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>Returns the async-enumerable stream wrapped in a completed task so the MediatR pipeline can dispatch it synchronously.</summary>
    public Task<IAsyncEnumerable<SseNotificationItem>> Handle(
        GetNotificationStreamQuery request,
        CancellationToken ct
    )
    {
        return Task.FromResult(StreamNotifications(request.Request.Count, ct));
    }

    /// <summary>Yields <paramref name="count"/> notification items at 500 ms intervals, honouring cancellation between each yield.</summary>
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
