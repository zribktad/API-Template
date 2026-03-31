namespace Notifications.Domain.Interfaces;

/// <summary>
/// Generic read-side abstraction for in-process queues, allowing background consumers to drain
/// items without coupling to a specific transport implementation.
/// </summary>
/// <typeparam name="T">The type of item read from the queue.</typeparam>
public interface IQueueReader<out T> : SharedKernel.Application.Queue.IQueueReader<T>;
