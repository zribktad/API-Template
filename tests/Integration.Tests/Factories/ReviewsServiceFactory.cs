extern alias ReviewsApi;

using Integration.Tests.Fixtures;

namespace Integration.Tests.Factories;

public sealed class ReviewsServiceFactory : ServiceFactoryBase<ReviewsApi::Program>
{
    public ReviewsServiceFactory(SharedContainers containers)
        : base(containers) { }

    protected override string ServiceName => "Reviews";
    protected override string ConnectionStringKey => "ReviewsDb";
}
