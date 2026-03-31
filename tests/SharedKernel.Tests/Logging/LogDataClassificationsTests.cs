using SharedKernel.Infrastructure.Logging;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Logging;

public sealed class LogDataClassificationsTests
{
    [Fact]
    public void PersonalDataAttribute_MapsToPersonalClassification()
    {
        PersonalDataAttribute attribute = new();

        attribute.Classification.ShouldBe(LogDataClassifications.Personal);
    }

    [Fact]
    public void SensitiveDataAttribute_MapsToSensitiveClassification()
    {
        SensitiveDataAttribute attribute = new();

        attribute.Classification.ShouldBe(LogDataClassifications.Sensitive);
    }
}
