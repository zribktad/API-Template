using Microsoft.Extensions.Compliance.Classification;

namespace SharedKernel.Application.Logging;

public sealed class SensitiveDataAttribute : DataClassificationAttribute
{
    public SensitiveDataAttribute()
        : base(LogDataClassifications.SensitiveData) { }
}
