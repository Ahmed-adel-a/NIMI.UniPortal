using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Attendance.Domain.Devices;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Attendance.Application.Devices.RequestChange;

/// <summary>
/// P6.8 — one pending request at a time (409) and at most one approved change per term (R11, 400). The domain checks
/// the fingerprint and the reason.
/// </summary>
internal sealed class RequestDeviceChangeHandler(IAttendanceDbContext db, ISchedulingApi scheduling, TimeProvider clock)
    : IRequestHandler<RequestDeviceChangeCommand, DeviceChangeDto>
{
    public async Task<DeviceChangeDto> Handle(RequestDeviceChangeCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsStudent)
            throw new ForbiddenException("Only students change their attendance device.");

        var pending = await db.DeviceChangeRequests.AsNoTracking()
            .AnyAsync(r => r.StudentId == command.StudentId && r.Status == DeviceChangeStatus.Pending, cancellationToken);
        if (pending)
            throw new ConflictException("You already have a pending device change request.");

        if (await scheduling.GetCurrentTermAsync(cancellationToken) is { } term)
        {
            var from = new DateTimeOffset(term.StartsOn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var to = new DateTimeOffset(term.EndsOn.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var approvedThisTerm = await db.DeviceChangeRequests.AsNoTracking()
                .AnyAsync(
                    r => r.StudentId == command.StudentId &&
                         r.Status == DeviceChangeStatus.Approved &&
                         r.DecidedAt >= from &&
                         r.DecidedAt < to,
                    cancellationToken);
            if (approvedThisTerm)
                throw ValidationErrors.For("newFingerprint", "Only one device change is allowed per term.");
        }

        var request = DeviceChangeRequest.Create(command.StudentId, command.NewFingerprint, command.Reason, clock.GetUtcNow());
        db.DeviceChangeRequests.Add(request);

        return new DeviceChangeDto(request.Id, AttendanceWire.DeviceStatus(request.Status));
    }
}
