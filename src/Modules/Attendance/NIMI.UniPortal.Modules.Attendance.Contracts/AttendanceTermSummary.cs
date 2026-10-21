namespace NIMI.UniPortal.Modules.Attendance.Contracts;

/// <summary>Average attendance (null before any closed session) and the number of distinct barred students.</summary>
public sealed record AttendanceTermSummary(decimal? AverageRate, int BarredStudents);
