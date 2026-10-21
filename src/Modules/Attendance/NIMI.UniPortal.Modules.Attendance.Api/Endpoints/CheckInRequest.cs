namespace NIMI.UniPortal.Modules.Attendance.Api.Endpoints;

/// <summary>POST /api/attendance/check-in.</summary>
public sealed record CheckInRequest(string Token, string DeviceFingerprint);
