namespace NIMI.UniPortal.Modules.Attendance.Domain.Records;

/// <summary>The status of one student in one session (phase §4).</summary>
public enum AttendanceStatus
{
    Present,
    Late,
    ManualAudit,
    Absent,
}
