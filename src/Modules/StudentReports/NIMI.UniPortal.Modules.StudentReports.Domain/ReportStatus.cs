namespace NIMI.UniPortal.Modules.StudentReports.Domain;

/// <summary>Open → answered (one reply) → closed; a report can also be closed straight from open.</summary>
public enum ReportStatus
{
    Open = 0,
    Answered = 1,
    Closed = 2,
}
