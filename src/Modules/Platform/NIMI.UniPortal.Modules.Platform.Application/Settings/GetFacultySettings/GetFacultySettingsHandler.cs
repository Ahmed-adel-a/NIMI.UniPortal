using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.GetFacultySettings;

internal sealed class GetFacultySettingsHandler(IPlatformDbContext db, IUsersApi users, IPlatformSettingsApi settings)
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

        return new FacultySettingsDto(
            query.FacultyId,
            Overrides(row?.Settings),
            EffectiveSettingsDto.From(await settings.GetEffectiveSettingsAsync(query.FacultyId, cancellationToken)),
            row?.Version,
            row?.Settings?.UpdatedAt);
    }

    private static FacultySettingsOverridesDto Overrides(FacultySettings? settings) =>
        new(settings?.WeekStartDay,
            settings?.WorkingDays,
            settings?.AttendanceThreshold,
            settings?.LateGraceMinutes,
            settings?.GradingScale?.Select(b => new GradeBandDto(b.Grade, b.MinScore, b.Points)).ToArray(),
            settings?.StudentCodeFormat,
            settings?.MilitaryEducationPolicy is { } policy ? new MilitaryEducationPolicyDto(policy.Mode) : null);
}
