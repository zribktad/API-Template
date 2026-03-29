extern alias FileStorageApi;

using Integration.Tests.Fixtures;

namespace Integration.Tests.Factories;

public sealed class FileStorageServiceFactory : ServiceFactoryBase<FileStorageApi::Program>
{
    public FileStorageServiceFactory(SharedContainers containers)
        : base(containers) { }

    protected override string ServiceName => "FileStorage";
    protected override string ConnectionStringKey => "FileStorageDb";
}
