using NIMI.UniPortal.Modules.Attendance.Domain.Records;

namespace NIMI.UniPortal.Modules.Attendance.Application.Common;

/// <summary>A group's closed sessions and how many of them each student attended (rule R9).</summary>
internal sealed record GroupAttendance(int Held, IReadOnlyDictionary<Guid, int> AttendedByStudent)
{
    public int AttendedBy(Guid studentId) => AttendedByStudent.GetValueOrDefault(studentId);

    public decimal PercentageOf(Guid studentId) => AttendanceRate.Percentage(AttendedBy(studentId), Held);
}
