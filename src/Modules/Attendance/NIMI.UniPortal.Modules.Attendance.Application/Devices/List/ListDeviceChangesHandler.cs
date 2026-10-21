using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Application.Common;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Attendance.Application.Devices.List;

internal sealed class ListDeviceChangesHandler(IAttendanceDbContext db, IUsersApi users)
    : IRequestHandler<ListDeviceChangesQuery, PagedResult<DeviceChangeItemDto>>
{
    public async Task<PagedResult<DeviceChangeItemDto>> Handle(ListDeviceChangesQuery query, CancellationToken cancellationToken)
    {
        var requests = db.DeviceChangeRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!AttendanceWire.TryParseDeviceChangeStatus(query.Status, out var status))
                throw ValidationErrors.For("status", "Status must be pending, approved or rejected.");
            requests = requests.Where(r => r.Status == status);
        }

        var total = await requests.CountAsync(cancellationToken);
        var page = await requests
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<DeviceChangeItemDto>(page.Count);
        foreach (var request in page)
        {
            var student = await users.GetUserAsync(request.StudentId, cancellationToken);
            items.Add(new DeviceChangeItemDto(
                request.Id,
                request.StudentId,
                student?.FullName ?? string.Empty,
                request.NewFingerprint,
                request.Reason,
                AttendanceWire.DeviceStatus(request.Status),
                request.CreatedAt));
        }

        return new PagedResult<DeviceChangeItemDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
