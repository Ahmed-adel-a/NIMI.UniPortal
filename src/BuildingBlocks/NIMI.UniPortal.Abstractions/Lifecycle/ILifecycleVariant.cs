namespace NIMI.UniPortal.Abstractions.Lifecycle;

/// <summary>
/// Decision LIFECYCLEDEPT: one academic system's answers (credit hours or year groups), over its own tables only.
/// Every deployment registers both; the host's router combines them into <see cref="IAcademicLifecycle"/>.
/// </summary>
public interface ILifecycleVariant
{
    /// <summary>The academic system this variant serves (<c>credit_hours</c> or <c>year_groups</c>).</summary>
    string AcademicSystem { get; }

    Task<IReadOnlyList<Guid>> GetStudentsInGroupAsync(Guid subjectGroupId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetGroupsForStudentAsync(Guid studentId, Guid termId, CancellationToken cancellationToken);

    Task<bool> IsStudentInGroupAsync(Guid studentId, Guid subjectGroupId, CancellationToken cancellationToken);

    Task<StudentProgress> GetProgressAsync(Guid studentId, CancellationToken cancellationToken);

    Task<int> CountConfirmedRegistrationsAsync(
        Guid termId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken);

    /// <summary>This system's students in the scope who cannot graduate yet, by its own rules.</summary>
    Task<int> CountGraduationBlockedAsync(Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken);

    Task<bool> HasStudentRecordsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken);
}
