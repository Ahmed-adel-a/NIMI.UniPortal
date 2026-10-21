namespace NIMI.UniPortal.Modules.Attendance.Contracts;

/// <summary>What other modules (and the host's dashboards, P10) may read from Attendance.</summary>
public interface IAttendanceApi
{
    /// <summary>
    /// The student's overall rate (0–100) over the closed sessions of a term (rule R9: present, late and
    /// manual_audit count as attended); null when no session of theirs has been closed yet.
    /// </summary>
    Task<decimal?> GetStudentRateAsync(Guid studentId, Guid termId, CancellationToken cancellationToken = default);
}
