namespace APITemplate.Application.Common.Startup;

public interface IStartupTaskCoordinator
{
    Task<IAsyncDisposable> AcquireAsync(
        StartupTaskName startupTask,
        CancellationToken ct = default
    );
}
