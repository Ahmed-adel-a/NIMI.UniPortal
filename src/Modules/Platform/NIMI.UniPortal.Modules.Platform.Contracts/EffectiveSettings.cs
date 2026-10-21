namespace NIMI.UniPortal.Modules.Platform.Contracts;

/// <summary>
/// The academic rules that actually apply to a faculty (P1.4): the university defaults with the faculty's
/// overrides on top. Resolution is field by field — the override when present, otherwise the system default;
/// an absent override row (or a null <see cref="FacultyId"/>) means the university defaults.
/// </summary>
public sealed record EffectiveSettings(
    Guid? FacultyId,
    string TimeZone,
    int WeekStartDay,
    int[] WorkingDays,
    decimal AttendanceThreshold,
    int LateGraceMinutes,
    IReadOnlyList<GradeBandRef> GradingScale,
    string StudentCodeFormat,
    string MilitaryEducationMode);
