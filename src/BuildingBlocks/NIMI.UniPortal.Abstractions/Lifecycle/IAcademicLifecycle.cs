namespace NIMI.UniPortal.Abstractions.Lifecycle;

/// <summary>
/// The seam between the academic lifecycle variant (credit hours today, semester-based later) and the core modules
/// (P4, decision LIFECYCLE). Core modules — attendance, materials, grades — learn rosters and progress only through
/// this contract, never through a lifecycle module's tables. Exactly one variant is composed per host.
/// </summary>
public interface IAcademicLifecycle
{
    /// <summary>Students who are confirmed members of the group (R11: registered/completed/failed only).</summary>
    Task<IReadOnlyList<Guid>> GetStudentsInGroupAsync(
        Guid subjectGroupId, CancellationToken cancellationToken = default);

    /// <summary>The groups the student is a confirmed member of in the term (used by the student schedule).</summary>
    Task<IReadOnlyList<Guid>> GetGroupsForStudentAsync(
        Guid studentId, Guid termId, CancellationToken cancellationToken = default);

    /// <summary>Whether the student is a confirmed member of the group (ownership checks for later phases).</summary>
    Task<bool> IsStudentInGroupAsync(
        Guid studentId, Guid subjectGroupId, CancellationToken cancellationToken = default);

    /// <summary>Credits earned/in progress and outstanding graduation requirements (P4.6).</summary>
    Task<StudentProgress> GetProgressAsync(Guid studentId, CancellationToken cancellationToken = default);
}
