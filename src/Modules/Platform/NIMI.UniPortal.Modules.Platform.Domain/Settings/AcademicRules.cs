namespace NIMI.UniPortal.Modules.Platform.Domain.Settings;

/// <summary>
/// The academic-rule validators shared by the university defaults (<see cref="SystemSettings"/>) and the
/// per-faculty overrides (<see cref="FacultySettings"/>, P1.4). Values are normalised on the way in, and every
/// rejection carries the JSON field name as <see cref="ArgumentException.ParamName"/>, so the host answers 400
/// naming that field. Week days are numbered 0 = Saturday … 6 = Friday everywhere in the product.
/// </summary>
public static class AcademicRules
{
    /// <summary>0 = Saturday … 6 = Friday.</summary>
    public static int WeekStartDay(int value, string paramName) =>
        value is >= 0 and <= 6
            ? value
            : throw new ArgumentOutOfRangeException(paramName, value, "0 = Saturday … 6 = Friday.");

    /// <summary>At least one working day, no repeats; stored ascending.</summary>
    public static int[] WorkingDays(IReadOnlyCollection<int> values, string paramName)
    {
        if (values is null || values.Count == 0)
            throw new ArgumentException("At least one working day.", paramName);
        foreach (var day in values)
            WeekStartDay(day, paramName);
        if (values.Distinct().Count() != values.Count)
            throw new ArgumentException("Working days must not repeat.", paramName);
        return [.. values.Order()];
    }

    /// <summary>Required attendance percentage (0–100).</summary>
    public static decimal AttendanceThreshold(decimal value, string paramName) =>
        InRange(value, 0m, 100m, paramName);

    /// <summary>Minutes after a lecture starts before a check-in counts as late (0–180).</summary>
    public static int LateGraceMinutes(int value, string paramName) =>
        (int)InRange(value, 0, 180, paramName);

    /// <summary>Valid grading bands ordered by minimum score (highest first); one band must start at 0.</summary>
    public static IReadOnlyList<GradeBand> GradingScale(IReadOnlyList<GradeBand> bands, string paramName)
    {
        if (bands is null || bands.Count == 0)
            throw new ArgumentException("The grading scale needs at least one band.", paramName);
        foreach (var band in bands)
        {
            Required(band.Grade, paramName);
            InRange(band.MinScore, 0m, 100m, paramName);
            InRange(band.Points, 0m, 5m, paramName);
        }

        if (bands.Select(b => b.Grade.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != bands.Count)
            throw new ArgumentException("Grades must be unique.", paramName);
        if (bands.Select(b => b.MinScore).Distinct().Count() != bands.Count)
            throw new ArgumentException("Two bands share the same minimum score.", paramName);
        if (!bands.Any(b => b.MinScore == 0m))
            throw new ArgumentException("One band must start at 0 so every score gets a grade.", paramName);

        return [.. bands.OrderByDescending(b => b.MinScore).Select(b => b with { Grade = b.Grade.Trim() })];
    }

    /// <summary>The student-code format; it must contain a <c>{SEQ}</c> token so codes stay unique.</summary>
    public static string StudentCodeFormat(string value, string paramName)
    {
        var format = Required(value, paramName);
        return format.Contains("{SEQ", StringComparison.Ordinal)
            ? format
            : throw new ArgumentException("The student-code format must contain a {SEQ} token so codes are unique.", paramName);
    }

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Required.", name) : value.Trim();

    private static decimal InRange(decimal value, decimal min, decimal max, string name) =>
        value >= min && value <= max ? value : throw new ArgumentOutOfRangeException(name, value, $"Must be between {min} and {max}.");
}
