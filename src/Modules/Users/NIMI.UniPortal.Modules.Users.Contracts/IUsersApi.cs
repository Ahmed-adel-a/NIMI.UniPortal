namespace NIMI.UniPortal.Modules.Users.Contracts;

/// <summary>
/// What other modules may ask the Users module (Contracts only — never its tables). Used e.g. by Platform to
/// validate faculty overrides (P1.4) and by Scheduling to check departments and teaching staff (P3).
/// </summary>
public interface IUsersApi
{
    Task<FacultyRef?> GetFacultyAsync(Guid facultyId, CancellationToken cancellationToken = default);

    Task<DepartmentRef?> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    Task<UserRef?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Active students only; a department filter wins over a faculty filter, both null means the university.</summary>
    Task<int> CountActiveStudentsAsync(Guid? facultyId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>The student's department/faculty/year, or null when the user is not a student.</summary>
    Task<StudentPlacementRef?> GetStudentPlacementAsync(Guid studentId, CancellationToken cancellationToken = default);
}
