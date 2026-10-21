using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Devices;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Devices.Decide;

/// <summary>
/// P6.8 — decides a pending request once (409 otherwise). Approving revokes the student's active phone and registers
/// the new one in the same save (the revoke is an UPDATE and runs before the INSERT, so the one-active-device index holds).
/// </summary>
internal sealed class DecideDeviceChangeHandler(IAttendanceDbContext db, TimeProvider clock)
    : IRequestHandler<DecideDeviceChangeCommand, DeviceChangeDto>
{
    public async Task<DeviceChangeDto> Handle(DecideDeviceChangeCommand command, CancellationToken cancellationToken)
    {
        var request = await db.DeviceChangeRequests
            .SingleOrDefaultAsync(r => r.Id == command.RequestId, cancellationToken)
            ?? throw new NotFoundException("Device change request not found.");

        if (request.Status != DeviceChangeStatus.Pending)
            throw new ConflictException("This device change request has already been decided.");

        var now = clock.GetUtcNow();
        if (!command.Approve)
        {
            request.Reject(command.DecidedBy, now);
            return new DeviceChangeDto(request.Id, AttendanceWire.DeviceStatus(request.Status));
        }

        request.Approve(command.DecidedBy, now);

        var active = await db.RegisteredDevices
            .Where(d => d.StudentId == request.StudentId && d.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var device in active)
            device.Revoke(now);

        db.RegisteredDevices.Add(RegisteredDevice.Register(request.StudentId, request.NewFingerprint, now));

        return new DeviceChangeDto(request.Id, AttendanceWire.DeviceStatus(request.Status));
    }
}
