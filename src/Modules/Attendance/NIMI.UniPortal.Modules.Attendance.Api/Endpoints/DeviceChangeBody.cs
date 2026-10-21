namespace NIMI.UniPortal.Modules.Attendance.Api.Endpoints;

/// <summary>POST /api/attendance/device-change.</summary>
public sealed record DeviceChangeBody(string? NewFingerprint, string? Reason);
