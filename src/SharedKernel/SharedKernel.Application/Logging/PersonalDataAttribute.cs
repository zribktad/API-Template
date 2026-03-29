using Microsoft.Extensions.Compliance.Classification;

namespace SharedKernel.Application.Logging;

public sealed class PersonalDataAttribute : DataClassificationAttribute
{
    public PersonalDataAttribute()
        : base(LogDataClassifications.PersonalData) { }
}
