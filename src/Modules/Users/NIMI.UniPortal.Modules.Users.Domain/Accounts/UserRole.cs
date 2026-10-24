namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>
/// The roles of the portal (decision ADMIN: one university-wide admin role; decision ROLES2: department head).
/// Their wire and database names are in <see cref="UserRoles"/>.
/// </summary>
public enum UserRole
{
    Student = 0,

    /// <summary>The whole teaching team (professor / lecturer / demonstrator — see <see cref="AcademicTitle"/>).</summary>
    Professor = 1,

    /// <summary>Student Affairs.</summary>
    Admin = 2,

    /// <summary>The supervisor of one department; receives its students' reports and may also teach.</summary>
    DepartmentHead = 3,
}
