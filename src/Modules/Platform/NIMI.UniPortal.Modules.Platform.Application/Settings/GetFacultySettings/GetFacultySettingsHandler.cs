using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetFacultySettings;

internal sealed class GetFacultySettingsHandler(IPlatformDbContext db, IUsersApi users)
    : IRequestHandler<GetFacultySettingsQuery, FacultySettingsDto>
{
    public async Task<FacultySettingsDto> Handle(GetFacultySettingsQuery query, CancellationToken cancellationToken)
    {
        // R1: the faculty must exist, even when it has no overrides yet.
        _ = await users.GetFacultyAsync(query.FacultyId, cancellationToken)
            ?? throw new NotFoundException("Faculty not found.");

        var row = await db.FacultySettings
            .AsNoTracking()
            .Where(f => f.FacultyId == query.FacultyId)
            .Select(f => new { Settings = f, Version = EF.Property<uint>(f, SettingsVersion.PropertyName) })
            .SingleOrDefaultAsync(cancellationToken);

        var system = await db.SystemSettings.AsNoTracking().SingleAsync(cancellationToken);
        var settings = row?.Settings;

        return new FacultySettingsDto(
            query.FacultyId,
            Overrides(settings),
            Effective(query.FacultyId, system, settings),
            row?.Version,
            settings?.UpdatedAt);
    }

    private static FacultySettingsOverridesDto Overrides(FacultySettings? settings) =>
        new(settings?.WeekStartDay,
            settings?.WorkingDays,
            settings?.AttendanceThreshold,
            settings?.LateGraceMinutes,
            settings?.GradingScale?.Select(b => new GradeBandDto(b.Grade, b.MinScore, b.Points)).ToArray(),
            settings?.StudentCodeFormat,
            settings?.MilitaryEducationPolicy is { } policy ? new MilitaryEducationPolicyDto(policy.Mode) : null);

    /// <summary>Field by field: the faculty value when it overrides, otherwise the university default (P1.4).</summary>
    private static EffectiveSettingsDto Effective(Guid facultyId, SystemSettings system, FacultySettings? settings) =>
        new(facultyId,
            system.TimeZone,
            settings?.WeekStartDay ?? system.WeekStartDay,
            settings?.WorkingDays ?? system.WorkingDays,
            settings?.AttendanceThreshold ?? system.AttendanceThreshold,
            settings?.LateGraceMinutes ?? system.LateGraceMinutes,
            [.. (settings?.GradingScale ?? system.GradingScale).Select(b => new GradeBandDto(b.Grade, b.MinScore, b.Points))],
            settings?.StudentCodeFormat ?? system.StudentCodeFormat,
            new MilitaryEducationPolicyDto(settings?.MilitaryEducationPolicy?.Mode ?? system.MilitaryEducationPolicy.Mode));
}
