using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.ManualEntry;

/// <summary>
/// P6.3 — the teaching professor records or overrides one student with a mandatory note (R7). Existing records may
/// only be changed while the session is open; status present/late is stored as <c>manual_audit</c>.
/// </summary>
internal sealed class ManualEntryHandler(
    IAttendanceDbContext db,
    IAcademicLifecycle lifecycle,
    ISchedulingApi scheduling,
    TimeProvider clock) : IRequestHandler<ManualEntryCommand, ManualEntryDto>
{
    public async Task<ManualEntryDto> Handle(ManualEntryCommand command, CancellationToken cancellationToken)
    {
        var session = await db.AttendanceSessions
            .SingleOrDefaultAsync(s => s.Id == command.SessionId, cancellationToken)
            ?? throw new NotFoundException("Attendance session not found.");

        await AttendanceAccess.EnsureTeachingAsync(scheduling, session, command.RecordedBy, cancellationToken);

        if (!session.IsOpen)
            throw new ConflictException("The session is closed.");

        if (!AttendanceWire.TryParseManualStatus(command.Status, out var requested))
            throw ValidationErrors.For(nameof(command.Status), "Status must be present, late or absent.");

        var isMember = await lifecycle.IsStudentInGroupAsync(
            command.StudentId, session.SubjectGroupId, cancellationToken);
        if (!isMember)
            throw ValidationErrors.For(nameof(command.StudentId), "Unknown student or not a member of this group.");

        var now = clock.GetUtcNow();
        var record = await db.AttendanceRecords
            .SingleOrDefaultAsync(
                r => r.SessionId == session.Id && r.StudentId == command.StudentId,
                cancellationToken);

        if (record is null)
        {
            record = AttendanceRecord.Manual(
                session.Id, command.StudentId, requested, command.Note, command.RecordedBy, now);
            db.AttendanceRecords.Add(record);
        }
        else
        {
            record.OverrideManually(requested, command.Note, command.RecordedBy, now);
        }

        return new ManualEntryDto(
            record.StudentId,
            AttendanceWire.Status(record.Status),
            AttendanceWire.Method(record.Method),
            record.Note!);
    }
}
