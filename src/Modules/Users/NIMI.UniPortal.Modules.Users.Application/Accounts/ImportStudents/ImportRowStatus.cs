namespace NIMI.UniPortal.Modules.Users.Application.Accounts.ImportStudents;

public enum ImportRowStatus
{
    Created = 0,

    /// <summary>The email already has an account — re-running a file is safe.</summary>
    Skipped = 1,

    Error = 2,
}
