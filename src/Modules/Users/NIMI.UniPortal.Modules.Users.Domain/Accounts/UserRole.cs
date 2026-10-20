namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>The three roles of the portal (decision ADMIN: one university-wide admin role).</summary>
public enum UserRole
{
    Student = 0,

    /// <summary>The whole teaching team (professor / lecturer / demonstrator — see <see cref="AcademicTitle"/>).</summary>
    Professor = 1,

    /// <summary>Student Affairs.</summary>
    Admin = 2,
}
