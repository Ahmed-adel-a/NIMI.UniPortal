namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.LiveRoster;

/// <summary>Totals per status for the live roster header.</summary>
public sealed record LiveRosterCountsDto(int Present, int Late, int ManualAudit, int Absent, int NotYet);
