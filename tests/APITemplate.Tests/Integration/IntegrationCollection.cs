using Xunit;

namespace APITemplate.Tests.Integration;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
