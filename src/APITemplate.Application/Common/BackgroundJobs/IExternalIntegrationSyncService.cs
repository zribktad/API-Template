namespace APITemplate.Application.Common.BackgroundJobs;

public interface IExternalIntegrationSyncService
{
    Task SynchronizeAsync(CancellationToken ct = default);
}
