namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>Teaching-staff rank. Display only (profile, forum badge) — it grants no permissions.</summary>
public enum AcademicTitle
{
    Professor = 0,
    Lecturer = 1,

    /// <summary>معيد.</summary>
    Demonstrator = 2,
}
