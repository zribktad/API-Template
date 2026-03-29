extern alias IdentityApi;

using Identity.Application.Security;
using Integration.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Integration.Tests.Factories;

public sealed class IdentityServiceFactory : ServiceFactoryBase<IdentityApi::Program>
{
    public IdentityServiceFactory(SharedContainers containers)
        : base(containers) { }

    protected override string ServiceName => "Identity";
    protected override string ConnectionStringKey => "IdentityDb";

    protected override void ConfigureServiceSpecificMocks(IServiceCollection services)
    {
        services.RemoveAll<IKeycloakAdminService>();
        services.AddSingleton(new Mock<IKeycloakAdminService>().Object);
    }
}
