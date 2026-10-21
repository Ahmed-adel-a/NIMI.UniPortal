namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.CloseSession;

/// <summary>The final tally of a closed session; closing again returns the same summary.</summary>
public sealed record CloseSummaryDto(
    Guid SessionId,
    DateTimeOffset ClosedAt,
    int Present,
    int Late,
    int ManualAudit,
    int Absent);
