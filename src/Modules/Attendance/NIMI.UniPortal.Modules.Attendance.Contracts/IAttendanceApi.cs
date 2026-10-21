namespace NIMI.UniPortal.Modules.Attendance.Contracts;

/// <summary>What other modules (and the host's dashboards, P10) may read from Attendance.</summary>
public interface IAttendanceApi
{
    /// <summary>
    /// The student's overall rate (0–100) over the closed sessions of a term (rule R9: present, late and
    /// manual_audit count as attended); null when no session of theirs has been closed yet.
    /// </summary>
    Task<decimal?> GetStudentRateAsync(Guid studentId, Guid termId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The term at a glance for the Student Affairs dashboard (P10.6): the average rate over every closed-session record
    /// and how many students are barred from at least one final (P10.5), optionally only for the given students.
    /// </summary>
    Task<AttendanceTermSummary> GetTermSummaryAsync(
        Guid termId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken = default);
}
