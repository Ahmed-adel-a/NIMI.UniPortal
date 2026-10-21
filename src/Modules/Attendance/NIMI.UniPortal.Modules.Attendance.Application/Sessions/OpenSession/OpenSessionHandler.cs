using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.OpenSession;

internal sealed class OpenSessionHandler(
    IAttendanceDbContext db,
    ISchedulingApi scheduling,
    IUsersApi users,
    IPlatformSettingsApi platform,
    TimeProvider clock) : IRequestHandler<OpenSessionCommand, AttendanceSessionDto>
{
    public async Task<AttendanceSessionDto> Handle(
        OpenSessionCommand command, CancellationToken cancellationToken)
    {
        var slot = await scheduling.GetSlotAsync(command.SlotId, cancellationToken);
        if (slot is null || slot.IsCancelled)
            throw ValidationErrors.For(nameof(command.SlotId), "Unknown or cancelled slot.");

        var group = await scheduling.GetGroupAsync(slot.GroupId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.SlotId), "The slot's group was not found.");
        var subject = await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.SlotId), "The slot's subject was not found.");
        var term = await scheduling.GetTermAsync(group.TermId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.SlotId), "The slot's term was not found.");

        var teaches = slot.StaffId == command.OpenedBy ||
                      await scheduling.IsTeachingAsync(
                          command.OpenedBy,
                          subject.Id,
                          term.Id,
                          group.Id,
                          cancellationToken);
        if (!teaches)
            throw new ForbiddenException("You do not teach this group.");

        var settings = await AttendanceSettings.ForDepartmentAsync(
            users, platform, subject.DepartmentId, cancellationToken);
        var zone = AttendanceSettings.Zone(settings);
        var sessionDate = command.Date
                          ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);

        var occurrences = await scheduling.GetOccurrencesAsync(
            [group.Id], term.StartsOn, term.EndsOn, cancellationToken);
        if (!occurrences.Any(o => o.SlotId == slot.Id && o.Date == sessionDate))
        {
            throw ValidationErrors.For(
                nameof(command.Date),
                "The date is not an occurrence of this slot in the term.");
        }

        var exists = await db.AttendanceSessions.AsNoTracking()
            .AnyAsync(s => s.SlotId == slot.Id && s.SessionDate == sessionDate, cancellationToken);
        if (exists)
            throw new ConflictException("A session already exists for this slot and date.");

        var session = AttendanceSession.Open(
            slot.Id,
            group.Id,
            subject.Id,
            term.Id,
            sessionDate,
            command.OpenedBy,
            clock.GetUtcNow());

        db.AttendanceSessions.Add(session);

        return new AttendanceSessionDto(
            session.Id,
            session.SlotId,
            session.SubjectGroupId,
            subject.Code,
            group.GroupNumber,
            session.SessionDate,
            session.OpenedAt,
            session.ClosedAt,
            session.IsOpen);
    }
}
