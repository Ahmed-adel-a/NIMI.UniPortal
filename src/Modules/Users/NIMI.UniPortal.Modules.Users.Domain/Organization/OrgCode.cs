using System.Text.RegularExpressions;

namespace NIMI.UniPortal.Modules.Users.Domain.Organization;

/// <summary>
/// Faculty and department codes (e.g. <c>ENG</c>, <c>CS</c>): 2–10 letters/digits, stored upper-case.
/// They end up inside student codes (<c>{FAC}</c> / <c>{DEPT}</c>), so they must stay short and clean.
/// </summary>
public static partial class OrgCode
{
    public static string Normalize(string code, string paramName)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        return CodePattern().IsMatch(normalized)
            ? normalized
            : throw new ArgumentException("Codes are 2–10 letters or digits, e.g. ENG or CS.", paramName);
    }

    public static string Name(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Required.", paramName);
        var trimmed = value.Trim();
        return trimmed.Length <= 255 ? trimmed : throw new ArgumentException("At most 255 characters.", paramName);
    }

    [GeneratedRegex("^[A-Z0-9]{2,10}$")]
    private static partial Regex CodePattern();
}
