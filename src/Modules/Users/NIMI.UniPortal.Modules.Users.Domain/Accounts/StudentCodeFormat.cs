using System.Globalization;
using System.Text.RegularExpressions;

namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>
/// Turns a student-code format into codes. Tokens: <c>{FAC}</c> faculty code, <c>{DEPT}</c> department code,
/// <c>{YEAR}</c> enrollment year, <c>{SEQ}</c> / <c>{SEQ:0000}</c> the running number (zero-padded to the given width).
/// Each distinct prefix counts on its own: <c>CS-2026-0001</c> and <c>CIV-2026-0001</c> can coexist.
/// </summary>
public static partial class StudentCodeFormat
{
    /// <summary>The counter key: the code with its number replaced by <c>#</c>, e.g. <c>CS-2026-#</c>.</summary>
    public static string CounterKey(string format, string facultyCode, string departmentCode, int enrollmentYear) =>
        SequenceToken().Replace(Fill(format, facultyCode, departmentCode, enrollmentYear), "#");

    public static string Render(string format, string facultyCode, string departmentCode, int enrollmentYear, int sequence)
    {
        if (sequence < 1)
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Sequences start at 1.");

        return SequenceToken().Replace(Fill(format, facultyCode, departmentCode, enrollmentYear), match =>
        {
            var width = match.Groups["width"].Success ? match.Groups["width"].Value.Length : 1;
            return sequence.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
        });
    }

    private static string Fill(string format, string facultyCode, string departmentCode, int enrollmentYear)
    {
        if (SequenceToken().Count(format) != 1)
            throw new ArgumentException("The student-code format needs exactly one {SEQ} token.", nameof(format));

        return format
            .Replace("{FAC}", facultyCode, StringComparison.Ordinal)
            .Replace("{DEPT}", departmentCode, StringComparison.Ordinal)
            .Replace("{YEAR}", enrollmentYear.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\{SEQ(?::(?<width>0+))?\}")]
    private static partial Regex SequenceToken();
}
