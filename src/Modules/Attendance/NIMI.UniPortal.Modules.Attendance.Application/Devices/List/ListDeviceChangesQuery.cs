using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Devices.List;

/// <summary>P6.8: Student Affairs' queue of device change requests, oldest first, optionally by status.</summary>
public sealed record ListDeviceChangesQuery(string? Status, PageRequest Paging) : IQuery<PagedResult<DeviceChangeItemDto>>;
