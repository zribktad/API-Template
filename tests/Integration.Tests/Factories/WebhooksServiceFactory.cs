extern alias WebhooksApi;

using Integration.Tests.Fixtures;

namespace Integration.Tests.Factories;

public sealed class WebhooksServiceFactory : ServiceFactoryBase<WebhooksApi::Program>
{
    public WebhooksServiceFactory(SharedContainers containers)
        : base(containers) { }

    protected override string ServiceName => "Webhooks";
    protected override string ConnectionStringKey => "DefaultConnection";
}
