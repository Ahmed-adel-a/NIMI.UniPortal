using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Platform.Application.Settings.UpdateFacultySettings;

internal sealed class UpdateFacultySettingsHandler(
    IPlatformDbContext db, IUsersApi users, IEventCollector events, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<UpdateFacultySettingsCommand, Unit>
{
    public async Task<Unit> Handle(UpdateFacultySettingsCommand command, CancellationToken cancellationToken)
    {
        // R1: overrides are only stored for a faculty that exists.
        var faculty = await users.GetFacultyAsync(command.FacultyId, cancellationToken)
            ?? throw new NotFoundException("Faculty not found.");

        var settings = await db.FacultySettings
            .SingleOrDefaultAsync(f => f.FacultyId == command.FacultyId, cancellationToken);

        if (settings is null)
        {
            if (!HasAnyValue(command))
                return Unit.Value;   // nothing to store

            settings = FacultySettings.Create(command.FacultyId, clock.GetUtcNow());
            db.FacultySettings.Add(settings);
        }
        else
        {
            // R3: overwriting an existing row requires the version the admin loaded; the UnitOfWork turns a
            // mismatch into 409 (like the university settings).
            if (command.Version is null)
                throw ValidationErrors.For(nameof(command.Version), "Send the version you loaded (GET the faculty settings first).");

            db.Entry(settings).Property(SettingsVersion.PropertyName).OriginalValue = command.Version.Value;
        }

        settings.Override(
            command.WeekStartDay,
            command.WorkingDays,
            command.AttendanceThreshold,
            command.LateGraceMinutes,
            command.GradingScale?.Select(b => new GradeBand(b.Grade, b.MinScore, b.Points)).ToArray(),
            command.StudentCodeFormat,
            command.MilitaryEducationPolicy is { } policy ? new MilitaryEducationPolicy(policy.Mode) : null,
            clock.GetUtcNow());

        // R4: clearing every override deletes the row instead of storing an empty one.
        if (!settings.HasOverrides)
            db.FacultySettings.Remove(settings);

        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            currentUser.UserId,
            AuditActions.FacultySettingsUpdated,
            AuditEntities.Faculty,
            faculty.Id,
            $"Academic-rule overrides of faculty {faculty.Code} updated."));

        return Unit.Value;
    }

    private static bool HasAnyValue(UpdateFacultySettingsCommand command) =>
        command.WeekStartDay is not null
        || command.WorkingDays is not null
        || command.AttendanceThreshold is not null
        || command.LateGraceMinutes is not null
        || command.GradingScale is not null
        || command.StudentCodeFormat is not null
        || command.MilitaryEducationPolicy is not null;
}
