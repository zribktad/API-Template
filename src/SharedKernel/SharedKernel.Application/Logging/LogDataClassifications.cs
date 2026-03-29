using Microsoft.Extensions.Compliance.Classification;

namespace SharedKernel.Application.Logging;

public static class LogDataClassifications
{
    public static DataClassification SensitiveData { get; } = new("Taxonomy", "SensitiveData");
    public static DataClassification PersonalData { get; } = new("Taxonomy", "PersonalData");

    public static DataClassificationSet Sensitive { get; } = new(SensitiveData);
    public static DataClassificationSet Personal { get; } = new(PersonalData);
}
