namespace SharedKernel.Infrastructure.Startup;

/// <summary>
/// Coordinates startup tasks across multiple service instances to prevent concurrent execution.
/// </summary>
public interface IStartupTaskCoordinator
{
    /// <summary>
    /// Acquires an exclusive lease for the specified startup task.
    /// The lease is released when the returned <see cref="IAsyncDisposable"/> is disposed.
    /// </summary>
    Task<IAsyncDisposable> AcquireAsync(
        StartupTaskName task,
        CancellationToken cancellationToken = default
    );
}
