using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;

namespace NIMI.UniPortal.Modules.Platform.Application;

/// <summary>Platform's public read contract for other modules: settings, faculty overrides and the effective rules (P1.4).</summary>
internal sealed class PlatformSettingsApi(IPlatformDbContext db) : IPlatformSettingsApi
{
    public async Task<string> GetStudentCodeFormatAsync(Guid facultyId, CancellationToken cancellationToken = default)
    {
        // R7: the faculty's format when it overrides one; the university format otherwise. Codes already issued
        // never change — a new format only changes the counter key used for new codes (CODECOUNTER).
        var overridden = await db.FacultySettings
            .AsNoTracking()
            .Where(f => f.FacultyId == facultyId)
            .Select(f => f.StudentCodeFormat)
            .SingleOrDefaultAsync(cancellationToken);

        return overridden
            ?? await db.SystemSettings.AsNoTracking().Select(s => s.StudentCodeFormat).SingleAsync(cancellationToken);
    }

    public async Task<EffectiveSettings> GetEffectiveSettingsAsync(Guid? facultyId, CancellationToken cancellationToken = default)
    {
        var system = await db.SystemSettings.AsNoTracking().SingleAsync(cancellationToken);
        var faculty = facultyId is null
            ? null
            : await db.FacultySettings
                .AsNoTracking()
                .SingleOrDefaultAsync(f => f.FacultyId == facultyId.Value, cancellationToken);

        // R6: field by field — the override when it is set, otherwise the university default. Timezone and
        // branding are never overridable (R8).
        return new EffectiveSettings(
            facultyId,
            system.TimeZone,
            faculty?.WeekStartDay ?? system.WeekStartDay,
            faculty?.WorkingDays ?? system.WorkingDays,
            faculty?.AttendanceThreshold ?? system.AttendanceThreshold,
            faculty?.LateGraceMinutes ?? system.LateGraceMinutes,
            [.. (faculty?.GradingScale ?? system.GradingScale).Select(b => new GradeBandRef(b.Grade, b.MinScore, b.Points))],
            faculty?.StudentCodeFormat ?? system.StudentCodeFormat,
            ModeName(faculty?.MilitaryEducationPolicy?.Mode ?? system.MilitaryEducationPolicy.Mode));
    }

    /// <summary>The same spelling the HTTP API uses for the enum ("opt_in" / "mandatory").</summary>
    private static string ModeName(MilitaryEducationMode mode) =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(mode.ToString());
}
