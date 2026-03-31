using Microsoft.Extensions.Compliance.Classification;

namespace SharedKernel.Infrastructure.Logging;

/// <summary>
/// Project-wide data classifications used by the compliance redaction pipeline.
/// </summary>
public static class LogDataClassifications
{
    private const string TaxonomyName = "APITemplate";

    public static DataClassification Personal => new(TaxonomyName, nameof(Personal));

    public static DataClassification Sensitive => new(TaxonomyName, nameof(Sensitive));

    public static DataClassification Public => new(TaxonomyName, nameof(Public));
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class PersonalDataAttribute : DataClassificationAttribute
{
    public PersonalDataAttribute()
        : base(LogDataClassifications.Personal) { }
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class SensitiveDataAttribute : DataClassificationAttribute
{
    public SensitiveDataAttribute()
        : base(LogDataClassifications.Sensitive) { }
}
