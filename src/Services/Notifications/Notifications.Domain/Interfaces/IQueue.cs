namespace Notifications.Domain.Interfaces;

/// <summary>
/// Generic write-side abstraction for in-process queues used to decouple producers from
/// background consumers without taking a dependency on a specific transport (e.g. Channel, Redis).
/// </summary>
/// <typeparam name="T">The type of item placed on the queue.</typeparam>
public interface IQueue<in T> : SharedKernel.Application.Queue.IQueue<T>;
