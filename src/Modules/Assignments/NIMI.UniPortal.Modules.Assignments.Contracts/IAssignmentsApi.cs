namespace NIMI.UniPortal.Modules.Assignments.Contracts;

/// <summary>What other modules (and the host's dashboards, P10) may read from Assignments.</summary>
public interface IAssignmentsApi
{
    /// <summary>
    /// The student's open assignments this term due before <paramref name="dueBefore"/> that they have not submitted,
    /// soonest first.
    /// </summary>
    Task<IReadOnlyList<UpcomingAssignmentRef>> GetUpcomingForStudentAsync(
        Guid studentId, DateTimeOffset dueBefore, CancellationToken cancellationToken = default);
}
