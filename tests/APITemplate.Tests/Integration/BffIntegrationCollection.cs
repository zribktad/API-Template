using Xunit;

namespace APITemplate.Tests.Integration;

[CollectionDefinition("Integration.Bff")]
public sealed class BffIntegrationCollection : ICollectionFixture<BffSecurityWebApplicationFactory>
{
}
