using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Devices;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Attendance.Application.CheckIn;

/// <summary>
/// P6.2 — verifies the rotating token, refuses outsiders, registers the first phone, applies the effective late
/// grace and writes the one record allowed for this student and session (R1–R5, DEVICE).
/// </summary>
internal sealed class CheckInHandler(
    IAttendanceDbContext db,
    IAcademicLifecycle lifecycle,
    ISchedulingApi scheduling,
    IUsersApi users,
    IPlatformSettingsApi platform,
    TimeProvider clock) : IRequestHandler<CheckInCommand, CheckInResultDto>
{
    public async Task<CheckInResultDto> Handle(CheckInCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsStudent)
            throw new ForbiddenException("Only students can check in.");

        if (!QrToken.TryRead(command.Token, out var sessionId, out _))
            throw ValidationErrors.For(nameof(command.Token), "The QR code is not valid.");

        var session = await db.AttendanceSessions
            .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.Token), "The QR code is not valid.");

        var now = clock.GetUtcNow();
        var tokenResult = QrToken.Verify(command.Token, session.Id, session.QrSecret, now);
        if (tokenResult == QrTokenResult.Expired)
            throw ValidationErrors.For(nameof(command.Token), "The QR code has expired.");
        if (tokenResult != QrTokenResult.Valid)
            throw ValidationErrors.For(nameof(command.Token), "The QR code is not valid.");

        if (!session.IsOpen)
            throw new ConflictException("The session is closed.");

        var isMember = await lifecycle.IsStudentInGroupAsync(
            command.StudentId, session.SubjectGroupId, cancellationToken);
        if (!isMember)
            throw new ForbiddenException("You are not in this group.");

        var fingerprint = command.DeviceFingerprint.Trim();
        var device = await db.RegisteredDevices
            .FirstOrDefaultAsync(
                d => d.StudentId == command.StudentId && d.RevokedAt == null,
                cancellationToken);
        if (device is not null && !string.Equals(device.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new ForbiddenException("Use your registered device.");

        var alreadyCheckedIn = await db.AttendanceRecords.AsNoTracking()
            .AnyAsync(
                r => r.SessionId == session.Id && r.StudentId == command.StudentId,
                cancellationToken);
        if (alreadyCheckedIn)
            throw new ConflictException("Already checked in.");

        var subject = await scheduling.GetSubjectAsync(session.SubjectId, cancellationToken)
            ?? throw new NotFoundException("The session's subject was not found.");
        var slot = await scheduling.GetSlotAsync(session.SlotId, cancellationToken)
            ?? throw new NotFoundException("The session's slot was not found.");
        var settings = await AttendanceSettings.ForDepartmentAsync(
            users, platform, subject.DepartmentId, cancellationToken);

        var isLate = LateRule.IsLate(
            now, session.SessionDate, slot.StartTime, settings.LateGraceMinutes, AttendanceSettings.Zone(settings));

        var record = AttendanceRecord.CheckIn(session.Id, command.StudentId, isLate, fingerprint, now);
        db.AttendanceRecords.Add(record);

        if (device is null)
            db.RegisteredDevices.Add(RegisteredDevice.Register(command.StudentId, fingerprint, now));

        return new CheckInResultDto(
            session.Id,
            AttendanceWire.Status(record.Status),
            record.CheckedInAt!.Value,
            subject.Code);
    }
}
