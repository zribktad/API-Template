extern alias NotificationsApi;

using Integration.Tests.Fixtures;

namespace Integration.Tests.Factories;

public sealed class NotificationsServiceFactory : ServiceFactoryBase<NotificationsApi::Program>
{
    public NotificationsServiceFactory(SharedContainers containers)
        : base(containers) { }

    protected override string ServiceName => "Notifications";
    protected override string ConnectionStringKey => "DefaultConnection";
}
