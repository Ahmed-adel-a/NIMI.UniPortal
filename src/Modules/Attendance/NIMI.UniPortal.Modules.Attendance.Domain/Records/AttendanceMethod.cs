namespace NIMI.UniPortal.Modules.Attendance.Domain.Records;

/// <summary>How an attendance record came to exist (phase §4).</summary>
public enum AttendanceMethod
{
    QrScan,
    ManualEntry,
    AutoAbsent,
}
