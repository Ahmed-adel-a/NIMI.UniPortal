namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions;

/// <summary>The short-lived token the professor's screen shows; <see cref="ExpiresAt"/> is issued-at + 20 seconds.</summary>
public sealed record SessionQrDto(string Token, DateTimeOffset ExpiresAt);
