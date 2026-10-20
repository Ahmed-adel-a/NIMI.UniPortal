using System.Text.RegularExpressions;

namespace NIMI.UniPortal.Modules.Platform.Domain.Settings;

/// <summary>
/// The university itself: branding + the default academic rules (decision SETTINGS).
/// Exactly one row per deployment database. Faculties may override academic rules (P1.4).
/// Week days are numbered 0 = Saturday … 6 = Friday everywhere in the product.
/// </summary>
public sealed partial class SystemSettings
{
    /// <summary>The single row's fixed id (seeded by the migration).</summary>
    public static readonly Guid SingletonId = Guid.Parse("0199a000-0000-7000-8000-0000000000f1");

    private SystemSettings()
    {
    }

    public Guid Id { get; private set; }

    public string UniversityName { get; private set; } = null!;

    public string UniversityNameAr { get; private set; } = null!;

    public string? Domain { get; private set; }

    public string? LogoUrl { get; private set; }

    public string PrimaryColor { get; private set; } = null!;

    public string SecondaryColor { get; private set; } = null!;

    /// <summary>IANA timezone for all wall-clock schedule/attendance math.</summary>
    public string TimeZone { get; private set; } = null!;

    public int WeekStartDay { get; private set; }

    public int[] WorkingDays { get; private set; } = [];

    /// <summary>Required attendance percentage (0–100).</summary>
    public decimal AttendanceThreshold { get; private set; }

    /// <summary>Minutes after a lecture starts before a check-in counts as late.</summary>
    public int LateGraceMinutes { get; private set; }

    public IReadOnlyList<GradeBand> GradingScale { get; private set; } = [];

    /// <summary>Tokens: {FAC}, {DEPT}, {YEAR}, {SEQ:0000}.</summary>
    public string StudentCodeFormat { get; private set; } = null!;

    public MilitaryEducationPolicy MilitaryEducationPolicy { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>The out-of-the-box row a new deployment starts with; the admin changes it via P0.2.</summary>
    public static SystemSettings CreateDefault(DateTimeOffset createdAt)
    {
        var settings = new SystemSettings { Id = SingletonId, CreatedAt = createdAt };
        settings.Apply(
            universityName: "University",
            universityNameAr: "الجامعة",
            domain: null,
            logoUrl: null,
            primaryColor: "#0A4CB3",
            secondaryColor: "#5B9BFF",
            timeZone: "Africa/Cairo",
            weekStartDay: 0,
            workingDays: [0, 1, 2, 3, 4, 5],   // Saturday – Thursday
            attendanceThreshold: 75m,
            lateGraceMinutes: 10,
            gradingScale: DefaultGradingScale,
            studentCodeFormat: "{DEPT}-{YEAR}-{SEQ:0000}",
            militaryEducationPolicy: new MilitaryEducationPolicy(MilitaryEducationMode.OptIn));
        return settings;
    }

    /// <summary>A common Egyptian 4.0 credit-hours scale. Only a default: each university sets its own.</summary>
    public static IReadOnlyList<GradeBand> DefaultGradingScale { get; } =
    [
        new("A+", 97m, 4.0m), new("A", 93m, 4.0m), new("A-", 89m, 3.7m),
        new("B+", 84m, 3.3m), new("B", 80m, 3.0m), new("B-", 76m, 2.7m),
        new("C+", 73m, 2.3m), new("C", 70m, 2.0m), new("C-", 67m, 1.7m),
        new("D+", 64m, 1.3m), new("D", 60m, 1.0m), new("F", 0m, 0.0m),
    ];

    /// <summary>Replaces every setting at once (PUT semantics). Throws <see cref="ArgumentException"/> on any invalid value.</summary>
    public void Update(
        string universityName,
        string universityNameAr,
        string? domain,
        string? logoUrl,
        string primaryColor,
        string secondaryColor,
        string timeZone,
        int weekStartDay,
        IReadOnlyCollection<int> workingDays,
        decimal attendanceThreshold,
        int lateGraceMinutes,
        IReadOnlyList<GradeBand> gradingScale,
        string studentCodeFormat,
        MilitaryEducationPolicy militaryEducationPolicy,
        DateTimeOffset updatedAt)
    {
        Apply(universityName, universityNameAr, domain, logoUrl, primaryColor, secondaryColor, timeZone,
            weekStartDay, workingDays, attendanceThreshold, lateGraceMinutes, gradingScale, studentCodeFormat,
            militaryEducationPolicy);
        UpdatedAt = updatedAt;
    }

    private void Apply(
        string universityName,
        string universityNameAr,
        string? domain,
        string? logoUrl,
        string primaryColor,
        string secondaryColor,
        string timeZone,
        int weekStartDay,
        IReadOnlyCollection<int> workingDays,
        decimal attendanceThreshold,
        int lateGraceMinutes,
        IReadOnlyList<GradeBand> gradingScale,
        string studentCodeFormat,
        MilitaryEducationPolicy militaryEducationPolicy)
    {
        UniversityName = Required(universityName, nameof(universityName));
        UniversityNameAr = Required(universityNameAr, nameof(universityNameAr));
        Domain = string.IsNullOrWhiteSpace(domain) ? null : domain.Trim();
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        PrimaryColor = HexColor(primaryColor, nameof(primaryColor));
        SecondaryColor = HexColor(secondaryColor, nameof(secondaryColor));
        TimeZone = IanaTimeZone(timeZone, nameof(timeZone));
        WeekStartDay = DayOfWeekNumber(weekStartDay, nameof(weekStartDay));
        WorkingDays = Days(workingDays, nameof(workingDays));
        AttendanceThreshold = InRange(attendanceThreshold, 0m, 100m, nameof(attendanceThreshold));
        LateGraceMinutes = (int)InRange(lateGraceMinutes, 0, 180, nameof(lateGraceMinutes));
        GradingScale = Scale(gradingScale, nameof(gradingScale));
        StudentCodeFormat = CodeFormat(studentCodeFormat, nameof(studentCodeFormat));
        MilitaryEducationPolicy = militaryEducationPolicy ?? throw new ArgumentNullException(nameof(militaryEducationPolicy));
    }

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Required.", name) : value.Trim();

    private static string HexColor(string value, string name) =>
        value is not null && HexColorPattern().IsMatch(value)
            ? value.ToUpperInvariant()
            : throw new ArgumentException("Must be a #RRGGBB color.", name);

    private static string IanaTimeZone(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || !TimeZoneInfo.TryFindSystemTimeZoneById(value, out var zone) || !zone.HasIanaId)
            throw new ArgumentException("Must be an IANA timezone id, e.g. Africa/Cairo.", name);
        return value;
    }

    private static int DayOfWeekNumber(int value, string name) =>
        value is >= 0 and <= 6 ? value : throw new ArgumentOutOfRangeException(name, value, "0 = Saturday … 6 = Friday.");

    private static int[] Days(IReadOnlyCollection<int> values, string name)
    {
        if (values is null || values.Count == 0)
            throw new ArgumentException("At least one working day.", name);
        foreach (var day in values)
            DayOfWeekNumber(day, name);
        if (values.Distinct().Count() != values.Count)
            throw new ArgumentException("Working days must not repeat.", name);
        return [.. values.Order()];
    }

    private static decimal InRange(decimal value, decimal min, decimal max, string name) =>
        value >= min && value <= max ? value : throw new ArgumentOutOfRangeException(name, value, $"Must be between {min} and {max}.");

    private static IReadOnlyList<GradeBand> Scale(IReadOnlyList<GradeBand> bands, string name)
    {
        if (bands is null || bands.Count == 0)
            throw new ArgumentException("The grading scale needs at least one band.", name);
        foreach (var band in bands)
        {
            Required(band.Grade, name);
            InRange(band.MinScore, 0m, 100m, name);
            InRange(band.Points, 0m, 5m, name);
        }

        if (bands.Select(b => b.Grade.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != bands.Count)
            throw new ArgumentException("Grades must be unique.", name);
        if (bands.Select(b => b.MinScore).Distinct().Count() != bands.Count)
            throw new ArgumentException("Two bands share the same minimum score.", name);
        if (!bands.Any(b => b.MinScore == 0m))
            throw new ArgumentException("One band must start at 0 so every score gets a grade.", name);

        return [.. bands.OrderByDescending(b => b.MinScore).Select(b => b with { Grade = b.Grade.Trim() })];
    }

    private static string CodeFormat(string value, string name)
    {
        var format = Required(value, name);
        return format.Contains("{SEQ", StringComparison.Ordinal)
            ? format
            : throw new ArgumentException("The student-code format must contain a {SEQ} token so codes are unique.", name);
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorPattern();
}
