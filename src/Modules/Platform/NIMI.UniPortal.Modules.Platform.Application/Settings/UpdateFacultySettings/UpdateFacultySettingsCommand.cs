using NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.UpdateFacultySettings;

/// <summary>
/// PUT semantics (P1.4): every field is the complete new set of overrides, and null means inherit.
/// <see cref="Version"/> is required only when the faculty already has overrides; a stale one → 409.
/// </summary>
public sealed record UpdateFacultySettingsCommand(
    Guid FacultyId,
    int? WeekStartDay,
    int[]? WorkingDays,
    decimal? AttendanceThreshold,
    int? LateGraceMinutes,
    GradeBandDto[]? GradingScale,
    string? StudentCodeFormat,
    MilitaryEducationPolicyDto? MilitaryEducationPolicy,
    uint? Version) : ICommand<Unit>;
