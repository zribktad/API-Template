namespace APITemplate.Application.Common.Specifications;

internal static class SpecificationSortingHelper
{
    internal static void ApplyOrder(bool desc, Action applyAsc, Action applyDesc)
    {
        if (desc) applyDesc();
        else applyAsc();
    }
}
