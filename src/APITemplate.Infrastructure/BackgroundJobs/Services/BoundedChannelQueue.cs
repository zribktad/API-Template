using System.Threading.Channels;

namespace APITemplate.Infrastructure.BackgroundJobs.Services;

/// <summary>
/// A generic bounded channel-based queue. Subclass or instantiate directly for
/// specific queue types (jobs, webhooks, emails, etc.).
/// </summary>
public class BoundedChannelQueue<T>
{
    private readonly Channel<T> _channel;

    public BoundedChannelQueue(int capacity)
    {
        _channel = Channel.CreateBounded<T>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
            }
        );
    }

    public IAsyncEnumerable<T> ReadAllAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAllAsync(ct);

    public ValueTask EnqueueAsync(T item, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(item, ct);
}
