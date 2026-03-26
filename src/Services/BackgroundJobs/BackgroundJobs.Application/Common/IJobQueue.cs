namespace BackgroundJobs.Application.Common;

/// <summary>
/// Generic write-side abstraction for in-process queues used to decouple producers from
/// background consumers without taking a dependency on a specific transport.
/// </summary>
/// <typeparam name="T">The type of item placed on the queue.</typeparam>
public interface IQueue<in T>
{
    /// <summary>
    /// Adds <paramref name="item"/> to the queue, waiting asynchronously if the queue is full.
    /// </summary>
    ValueTask EnqueueAsync(T item, CancellationToken ct = default);
}

/// <summary>
/// Generic read-side abstraction for in-process queues, allowing background consumers to drain
/// items without coupling to a specific transport implementation.
/// </summary>
/// <typeparam name="T">The type of item read from the queue.</typeparam>
public interface IQueueReader<out T>
{
    /// <summary>
    /// Returns an async stream that yields items as they become available, completing only when
    /// <paramref name="ct"/> is cancelled or the underlying channel is closed.
    /// </summary>
    IAsyncEnumerable<T> ReadAllAsync(CancellationToken ct = default);
}

/// <summary>
/// Write-side contract for enqueuing generic background job identifiers (as <see cref="Guid"/>s).
/// </summary>
public interface IJobQueue : IQueue<Guid>;

/// <summary>
/// Read-side contract for consuming job identifiers from the generic job queue.
/// </summary>
public interface IJobQueueReader : IQueueReader<Guid>;
