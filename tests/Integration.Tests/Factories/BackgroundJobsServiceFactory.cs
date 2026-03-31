extern alias BackgroundJobsApi;

using Integration.Tests.Fixtures;

namespace Integration.Tests.Factories;

public sealed class BackgroundJobsServiceFactory : ServiceFactoryBase<BackgroundJobsApi::Program>
{
    public BackgroundJobsServiceFactory(SharedContainers containers)
        : base(containers) { }

    protected override string ServiceName => "BackgroundJobs";
    protected override string ConnectionStringKey => "DefaultConnection";
}
