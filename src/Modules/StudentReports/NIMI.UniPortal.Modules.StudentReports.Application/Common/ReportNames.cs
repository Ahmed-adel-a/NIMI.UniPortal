using System.Text.Json;
using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Common;

/// <summary>The wire names of categories and statuses (<c>academic</c>, <c>answered</c>), read case-insensitively.</summary>
public static class ReportNames
{
    public static string Name<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    public static bool TryParse<TEnum>(string? text, out TEnum value)
        where TEnum : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (string.Equals(text?.Trim(), Name(candidate), StringComparison.OrdinalIgnoreCase))
            {
                value = candidate;
                return true;
            }
        }

        value = default;
        return false;
    }
}
