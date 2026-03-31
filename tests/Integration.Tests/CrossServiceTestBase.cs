using Integration.Tests.Fixtures;
using Xunit;

namespace Integration.Tests;

public static class TestConstants
{
    public const string CollectionName = "CrossService";
    public const string CategoryName = "Integration.CrossService";
    public const string StartupSmokeCategoryName = "Integration.SmokeStartup";
    public static readonly TimeSpan TrackedSessionTimeout = TimeSpan.FromSeconds(30);
}

[CollectionDefinition(TestConstants.CollectionName)]
public sealed class CrossServiceCollection : ICollectionFixture<SharedContainers>;
