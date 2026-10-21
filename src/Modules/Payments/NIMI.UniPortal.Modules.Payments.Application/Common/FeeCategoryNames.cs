using NIMI.UniPortal.Modules.Payments.Domain.Fees;

namespace NIMI.UniPortal.Modules.Payments.Application.Common;

/// <summary>Maps the API's <c>category</c> strings to <see cref="FeeCategory"/> and back.</summary>
internal static class FeeCategoryNames
{
    public static bool TryParse(string? value, out FeeCategory category)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "tuition":
                category = FeeCategory.Tuition;
                return true;
            case "military_education":
                category = FeeCategory.MilitaryEducation;
                return true;
            case "activity":
                category = FeeCategory.Activity;
                return true;
            case "other":
                category = FeeCategory.Other;
                return true;
            default:
                category = default;
                return false;
        }
    }

    public static string ToWire(FeeCategory category) => EnumWire.ToWire(category);
}
