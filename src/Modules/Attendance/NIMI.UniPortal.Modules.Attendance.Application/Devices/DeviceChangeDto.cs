namespace NIMI.UniPortal.Modules.Attendance.Application.Devices;

/// <summary>A device change request's id and state (<c>pending</c>, <c>approved</c>, <c>rejected</c>).</summary>
public sealed record DeviceChangeDto(Guid Id, string Status);
