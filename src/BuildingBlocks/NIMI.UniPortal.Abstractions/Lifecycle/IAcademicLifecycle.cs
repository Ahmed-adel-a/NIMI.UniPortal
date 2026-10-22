namespace NIMI.UniPortal.Abstractions.Lifecycle;

/// <summary>
/// The seam between the academic lifecycle and the core modules (P4). Core modules (attendance, materials, grades, …)
/// learn rosters and progress only through this contract, never through a lifecycle module's tables. Decision
/// LIFECYCLEDEPT: every deployment installs both variants (<see cref="ILifecycleVariant"/>); the host's router
/// implements this interface, combining both for group questions (mixed groups) and asking the student's own system
/// for student questions.
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

    /// <summary>
    /// Confirmed registrations (registered/completed/failed) in a term, optionally only of the given students — the
    /// Student Affairs dashboard (P10.6).
    /// </summary>
    Task<int> CountConfirmedRegistrationsAsync(
        Guid termId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active students (university, faculty or department) not yet eligible to graduate, by the variant's own rules —
    /// the Student Affairs dashboard (P10.6).
    /// </summary>
    Task<int> CountGraduationBlockedAsync(Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether any of the students has records in the given academic system (Platform refuses to move a department to
    /// another system while its students have history in the old one).
    /// </summary>
    Task<bool> HasStudentRecordsAsync(
        string academicSystem, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken = default);
}
