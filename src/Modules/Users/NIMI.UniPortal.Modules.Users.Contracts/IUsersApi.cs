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
}
