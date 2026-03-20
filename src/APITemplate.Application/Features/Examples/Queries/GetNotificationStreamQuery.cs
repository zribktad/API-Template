using System.Runtime.CompilerServices;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Examples.DTOs;

namespace APITemplate.Application.Features.Examples;

public sealed record GetNotificationStreamQuery(SseStreamRequest Request)
    : IQuery<IAsyncEnumerable<SseNotificationItem>>;

public sealed class GetNotificationStreamQueryHandler
    : IQueryHandler<GetNotificationStreamQuery, IAsyncEnumerable<SseNotificationItem>>
{
    private readonly TimeProvider _timeProvider;

    public GetNotificationStreamQueryHandler(TimeProvider timeProvider) =>
        _timeProvider = timeProvider;

    public Task<IAsyncEnumerable<SseNotificationItem>> HandleAsync(
        GetNotificationStreamQuery request,
        CancellationToken ct
    )
    {
        return Task.FromResult(StreamNotifications(request.Request.Count, ct));
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
