namespace NIMI.UniPortal.Modules.Platform.Domain.Settings;

/// <summary>
/// One faculty's overrides of the academic rules (P1.4): a null value means "inherit the university default"
/// (<see cref="SystemSettings"/>). There is at most one row per faculty, and it is deleted when every rule is
/// cleared — an empty row is never stored.
/// </summary>
public sealed class FacultySettings
{
    private FacultySettings()
    {
    }

    public Guid Id { get; private set; }

    public Guid FacultyId { get; private set; }

    public int? WeekStartDay { get; private set; }

    public int[]? WorkingDays { get; private set; }

    public decimal? AttendanceThreshold { get; private set; }

    public int? LateGraceMinutes { get; private set; }

    public IReadOnlyList<GradeBand>? GradingScale { get; private set; }

    public string? StudentCodeFormat { get; private set; }

    public MilitaryEducationPolicy? MilitaryEducationPolicy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>At least one rule is overridden — otherwise the row is deleted instead of kept empty.</summary>
    public bool HasOverrides =>
        WeekStartDay is not null || WorkingDays is not null || AttendanceThreshold is not null
        || LateGraceMinutes is not null || GradingScale is not null || StudentCodeFormat is not null
        || MilitaryEducationPolicy is not null;

    /// <summary>A fresh row that inherits every rule.</summary>
    public static FacultySettings Create(Guid facultyId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            FacultyId = facultyId,
            CreatedAt = now,
        };

    /// <summary>
    /// Replaces every override at once (PUT semantics); null means inherit. Every value is validated before any is
    /// stored, so a rejected call leaves the previous overrides untouched. The caller removes the row when
    /// <see cref="HasOverrides"/> turns false.
    /// </summary>
    public void Override(
        int? weekStartDay,
        IReadOnlyCollection<int>? workingDays,
        decimal? attendanceThreshold,
        int? lateGraceMinutes,
        IReadOnlyList<GradeBand>? gradingScale,
        string? studentCodeFormat,
        MilitaryEducationPolicy? militaryEducationPolicy,
        DateTimeOffset now)
    {
        var weekStart = weekStartDay is null
            ? null
            : (int?)AcademicRules.WeekStartDay(weekStartDay.Value, nameof(weekStartDay));
        var days = workingDays is null
            ? null
            : AcademicRules.WorkingDays(workingDays, nameof(workingDays));
        var threshold = attendanceThreshold is null
            ? null
            : (decimal?)AcademicRules.AttendanceThreshold(attendanceThreshold.Value, nameof(attendanceThreshold));
        var grace = lateGraceMinutes is null
            ? null
            : (int?)AcademicRules.LateGraceMinutes(lateGraceMinutes.Value, nameof(lateGraceMinutes));
        var scale = gradingScale is null
            ? null
            : AcademicRules.GradingScale(gradingScale, nameof(gradingScale));
        var format = studentCodeFormat is null
            ? null
            : AcademicRules.StudentCodeFormat(studentCodeFormat, nameof(studentCodeFormat));

        WeekStartDay = weekStart;
        WorkingDays = days;
        AttendanceThreshold = threshold;
        LateGraceMinutes = grace;
        GradingScale = scale;
        StudentCodeFormat = format;
        MilitaryEducationPolicy = militaryEducationPolicy;
        UpdatedAt = now;
    }
}
