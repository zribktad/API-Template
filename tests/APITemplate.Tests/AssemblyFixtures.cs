using APITemplate.Tests.Integration;
using APITemplate.Tests.Integration.Postgres;
using Xunit;

[assembly: AssemblyFixture(typeof(CustomWebApplicationFactory))]
[assembly: AssemblyFixture(typeof(SharedPostgresContainer))]
