namespace NIMI.UniPortal.Modules.Users.Domain.Accounts;

/// <summary>Brute-force protection (decision AUTH): 5 wrong passwords in a row lock the account for 15 minutes.</summary>
public static class LoginLockout
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(15);
}
