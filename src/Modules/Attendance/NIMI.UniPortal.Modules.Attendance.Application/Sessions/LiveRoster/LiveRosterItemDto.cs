namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.LiveRoster;

/// <summary>One student on the live roster; status is present, late, manual_audit, absent or not_yet.</summary>
public sealed record LiveRosterItemDto(Guid StudentId, string FullName, string? StudentCode, string Status);
