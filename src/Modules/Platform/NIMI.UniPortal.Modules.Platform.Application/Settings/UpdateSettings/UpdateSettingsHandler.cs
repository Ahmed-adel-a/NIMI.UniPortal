using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.UpdateSettings;

internal sealed class UpdateSettingsHandler(IPlatformDbContext db, TimeProvider clock) : IRequestHandler<UpdateSettingsCommand, Unit>
{
    public async Task<Unit> Handle(UpdateSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.SingleAsync(cancellationToken);

        // Optimistic concurrency: the UPDATE only succeeds if the row still has the version the admin loaded.
        db.Entry(settings).Property(SettingsVersion.PropertyName).OriginalValue = command.Version;

        settings.Update(
            command.UniversityName,
            command.UniversityNameAr,
            command.Domain,
            command.LogoUrl,
            command.PrimaryColor,
            command.SecondaryColor,
            command.TimeZone,
            command.WeekStartDay,
            command.WorkingDays,
            command.AttendanceThreshold,
            command.LateGraceMinutes,
            [.. command.GradingScale.Select(b => new GradeBand(b.Grade, b.MinScore, b.Points))],
            command.StudentCodeFormat,
            new MilitaryEducationPolicy(command.MilitaryEducationPolicy.Mode),
            clock.GetUtcNow());

        return Unit.Value;   // no SaveChanges here — the UnitOfWork behavior commits once
    }
}
