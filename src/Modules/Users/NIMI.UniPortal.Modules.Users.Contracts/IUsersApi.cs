using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Users.Contracts;

/// <summary>
/// What other modules may ask the Users module (Contracts only — never its tables). Used e.g. by Platform to
/// validate faculty overrides (P1.4) and by Scheduling to check departments and teaching staff (P3).
/// </summary>
public interface IUsersApi
{
    Task<FacultyRef?> GetFacultyAsync(Guid facultyId, CancellationToken cancellationToken = default);

    Task<DepartmentRef?> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>Every faculty, ordered by code (decision LIFECYCLEDEPT: academic systems per faculty).</summary>
    Task<IReadOnlyList<FacultyRef>> GetFacultiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every department, or those of one faculty, ordered by code.</summary>
    Task<IReadOnlyList<DepartmentRef>> GetDepartmentsAsync(Guid? facultyId, CancellationToken cancellationToken = default);

    Task<UserRef?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Several accounts at once (unknown ids are left out), e.g. to name the people in a list.</summary>
    Task<IReadOnlyList<UserRef>> GetUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>The department's active head (decision ROLES2), or null when it has none.</summary>
    Task<UserRef?> GetDepartmentHeadAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>Active students only; a department filter wins over a faculty filter, both null means the university.</summary>
    Task<int> CountActiveStudentsAsync(Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>The student's department/faculty/year, or null when the user is not a student.</summary>
    Task<StudentPlacementRef?> GetStudentPlacementAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active students ordered by student code (P9.0); a department filter wins over a faculty filter, both null
    /// means the university.
    /// </summary>
    Task<PagedResult<StudentRef>> GetStudentsAsync(
        Guid? facultyId, Guid? departmentId, PageRequest paging, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ids of every active student in the scope (P10.6 dashboard filters); department wins over faculty, both null
    /// means the university.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveStudentIdsAsync(
        Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken = default);
}
